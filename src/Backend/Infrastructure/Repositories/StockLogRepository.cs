using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class StockLogRepository : IStockLogRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public StockLogRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<StockLogDto>> GetAllAsync(long? medicineId = null, string? actionType = null)
    {
        var list = new List<StockLogDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        var sql = "SELECT sl.LogID, sl.MedicineID, m.MedicineName, sl.UserID, u.FullName AS UserName, " +
                  "sl.ActionType, sl.Quantity, sl.ActionDate, sl.Remarks " +
                  "FROM StockLog sl " +
                  "JOIN Medicine m ON sl.MedicineID = m.MedicineID " +
                  "JOIN Users u ON sl.UserID = u.UserID " +
                  "WHERE 1=1 ";

        if (medicineId.HasValue)
        {
            sql += "AND sl.MedicineID = :p_mid ";
        }
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            sql += "AND sl.ActionType = :p_atype ";
        }

        sql += "ORDER BY sl.LogID DESC";

        using var cmd = new OracleCommand(sql, conn);
        if (medicineId.HasValue)
        {
            cmd.Parameters.Add(new OracleParameter("p_mid", medicineId.Value));
        }
        if (!string.IsNullOrWhiteSpace(actionType))
        {
            cmd.Parameters.Add(new OracleParameter("p_atype", actionType.Trim()));
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new StockLogDto
            {
                LogID = Convert.ToInt64(reader["LogID"]),
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString(),
                UserID = Convert.ToInt64(reader["UserID"]),
                UserName = reader["UserName"].ToString(),
                ActionType = reader["ActionType"].ToString()!,
                Quantity = Convert.ToInt32(reader["Quantity"]),
                ActionDate = Convert.ToDateTime(reader["ActionDate"]),
                Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString()
            });
        }
        return list;
    }

    public async Task<StockLogDto> RecordAdjustmentAsync(CreateStockAdjustmentRequest request, long userId)
    {
        if (request.Quantity == 0)
            throw new ValidationException("Adjustment quantity cannot be zero.");

        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Verify medicine exists and get stock
            int currentStock;
            string medName;
            using (var medCmd = new OracleCommand("SELECT MedicineName, QuantityInStock FROM Medicine WHERE MedicineID = :p_id", conn))
            {
                medCmd.Transaction = tx;
                medCmd.Parameters.Add(new OracleParameter("p_id", request.MedicineID));
                using var reader = await medCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new BusinessRuleException($"Medicine with ID {request.MedicineID} does not exist.");

                medName = reader["MedicineName"].ToString()!;
                currentStock = Convert.ToInt32(reader["QuantityInStock"]);
            }

            var newStock = currentStock + request.Quantity;
            if (newStock < 0)
            {
                throw new BusinessRuleException(
                    $"Cannot adjust inventory. Current stock ({currentStock}) is insufficient for a decrease of {Math.Abs(request.Quantity)}.");
            }

            // Update Medicine stock
            using (var updateCmd = new OracleCommand("UPDATE Medicine SET QuantityInStock = :p_stock WHERE MedicineID = :p_id", conn))
            {
                updateCmd.Transaction = tx;
                updateCmd.Parameters.Add(new OracleParameter("p_stock", newStock));
                updateCmd.Parameters.Add(new OracleParameter("p_id", request.MedicineID));
                await updateCmd.ExecuteNonQueryAsync();
            }

            // Generate Log ID
            long logId;
            using (var idCmd = new OracleCommand("SELECT NVL(MAX(LogID), 0) + 1 FROM StockLog", conn))
            {
                idCmd.Transaction = tx;
                logId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
            }

            // Insert into StockLog
            var remarks = $"Manual adjustment ({request.Reason}): {(request.Quantity > 0 ? "+" : "")}{request.Quantity} units (From {currentStock} to {newStock})";
            using (var logCmd = new OracleCommand(
                "INSERT INTO StockLog (LogID, MedicineID, UserID, ActionType, Quantity, ActionDate, Remarks) " +
                "VALUES (:p_lid, :p_mid, :p_uid, 'Adjustment', :p_qty, SYSDATE, :p_rem)", conn))
            {
                logCmd.Transaction = tx;
                logCmd.Parameters.Add(new OracleParameter("p_lid", logId));
                logCmd.Parameters.Add(new OracleParameter("p_mid", request.MedicineID));
                logCmd.Parameters.Add(new OracleParameter("p_uid", userId));
                logCmd.Parameters.Add(new OracleParameter("p_qty", Math.Abs(request.Quantity)));
                logCmd.Parameters.Add(new OracleParameter("p_rem", remarks));
                await logCmd.ExecuteNonQueryAsync();
            }

            tx.Commit();

            return new StockLogDto
            {
                LogID = logId,
                MedicineID = request.MedicineID,
                MedicineName = medName,
                UserID = userId,
                ActionType = "Adjustment",
                Quantity = Math.Abs(request.Quantity),
                ActionDate = DateTime.UtcNow,
                Remarks = remarks
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
