using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public PurchaseRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var list = new List<PurchaseDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT p.PurchaseID, p.SupplierID, s.SupplierName, p.UserID, u.FullName AS UserName, " +
            "p.PurchaseDate, p.TotalAmount " +
            "FROM Purchase p " +
            "JOIN Supplier s ON p.SupplierID = s.SupplierID " +
            "JOIN Users u ON p.UserID = u.UserID " +
            "ORDER BY p.PurchaseID DESC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new PurchaseDto
            {
                PurchaseID = Convert.ToInt64(reader["PurchaseID"]),
                SupplierID = Convert.ToInt64(reader["SupplierID"]),
                SupplierName = reader["SupplierName"].ToString(),
                UserID = Convert.ToInt64(reader["UserID"]),
                UserName = reader["UserName"].ToString(),
                PurchaseDate = Convert.ToDateTime(reader["PurchaseDate"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
            });
        }
        return list;
    }

    public async Task<PurchaseDto?> GetByIdAsync(long purchaseId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        PurchaseDto? purchase = null;

        using (var cmd = new OracleCommand(
            "SELECT p.PurchaseID, p.SupplierID, s.SupplierName, p.UserID, u.FullName AS UserName, " +
            "p.PurchaseDate, p.TotalAmount " +
            "FROM Purchase p " +
            "JOIN Supplier s ON p.SupplierID = s.SupplierID " +
            "JOIN Users u ON p.UserID = u.UserID " +
            "WHERE p.PurchaseID = :p_id", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", purchaseId));
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                purchase = new PurchaseDto
                {
                    PurchaseID = Convert.ToInt64(reader["PurchaseID"]),
                    SupplierID = Convert.ToInt64(reader["SupplierID"]),
                    SupplierName = reader["SupplierName"].ToString(),
                    UserID = Convert.ToInt64(reader["UserID"]),
                    UserName = reader["UserName"].ToString(),
                    PurchaseDate = Convert.ToDateTime(reader["PurchaseDate"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                };
            }
        }

        if (purchase == null) return null;

        // Fetch detail items
        using (var detailCmd = new OracleCommand(
            "SELECT pd.PurchaseDetailID, pd.PurchaseID, pd.MedicineID, m.MedicineName, m.BatchNumber, " +
            "pd.Quantity, pd.UnitPrice, pd.SubTotal " +
            "FROM PurchaseDetails pd " +
            "JOIN Medicine m ON pd.MedicineID = m.MedicineID " +
            "WHERE pd.PurchaseID = :p_id " +
            "ORDER BY pd.PurchaseDetailID ASC", conn))
        {
            detailCmd.Parameters.Add(new OracleParameter("p_id", purchaseId));
            using var reader = await detailCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                purchase.Details.Add(new PurchaseDetailDto
                {
                    PurchaseDetailID = Convert.ToInt64(reader["PurchaseDetailID"]),
                    PurchaseID = Convert.ToInt64(reader["PurchaseID"]),
                    MedicineID = Convert.ToInt64(reader["MedicineID"]),
                    MedicineName = reader["MedicineName"].ToString(),
                    BatchNumber = reader["BatchNumber"].ToString(),
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                    SubTotal = Convert.ToDecimal(reader["SubTotal"])
                });
            }
        }

        return purchase;
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, long userId)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("Purchase must contain at least one medicine item.");

        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Verify Supplier exists
            using (var supCmd = new OracleCommand("SELECT COUNT(*) FROM Supplier WHERE SupplierID = :p_sup", conn))
            {
                supCmd.Transaction = tx;
                supCmd.Parameters.Add(new OracleParameter("p_sup", request.SupplierID));
                if (Convert.ToInt32(await supCmd.ExecuteScalarAsync()) == 0)
                    throw new BusinessRuleException($"Supplier with ID {request.SupplierID} does not exist.");
            }

            // Verify User exists
            using (var userCmd = new OracleCommand("SELECT COUNT(*) FROM Users WHERE UserID = :p_usr", conn))
            {
                userCmd.Transaction = tx;
                userCmd.Parameters.Add(new OracleParameter("p_usr", userId));
                if (Convert.ToInt32(await userCmd.ExecuteScalarAsync()) == 0)
                    throw new BusinessRuleException($"User with ID {userId} does not exist.");
            }

            // Generate next manual PurchaseID
            long purchaseId;
            using (var idCmd = new OracleCommand("SELECT NVL(MAX(PurchaseID), 0) + 1 FROM Purchase", conn))
            {
                idCmd.Transaction = tx;
                purchaseId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
            }

            // 1. If exactly one item is present, call Oracle Stored Procedure RECORD_PURCHASE
            if (request.Items.Count == 1)
            {
                var item = request.Items[0];
                if (item.Quantity <= 0) throw new BusinessRuleException("Purchase quantity must be greater than zero.");
                if (item.UnitPrice < 0) throw new BusinessRuleException("Unit price cannot be negative.");

                using var procCmd = new OracleCommand("RECORD_PURCHASE", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    Transaction = tx
                };
                procCmd.Parameters.Add(new OracleParameter("p_purchaseid", OracleDbType.Int64, purchaseId, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_supplierid", OracleDbType.Int64, request.SupplierID, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_userid", OracleDbType.Int64, userId, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_medicineid", OracleDbType.Int64, item.MedicineID, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_quantity", OracleDbType.Int32, item.Quantity, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_unitprice", OracleDbType.Decimal, item.UnitPrice, ParameterDirection.Input));

                await procCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Multi-item purchase: Insert header then insert detail rows
                // Triggers automatically compute SubTotal, increase stock, and maintain TotalAmount!
                using (var headerCmd = new OracleCommand(
                    "INSERT INTO Purchase (PurchaseID, SupplierID, UserID, PurchaseDate, TotalAmount) " +
                    "VALUES (:p_id, :p_sup, :p_usr, SYSDATE, 0)", conn))
                {
                    headerCmd.Transaction = tx;
                    headerCmd.Parameters.Add(new OracleParameter("p_id", purchaseId));
                    headerCmd.Parameters.Add(new OracleParameter("p_sup", request.SupplierID));
                    headerCmd.Parameters.Add(new OracleParameter("p_usr", userId));
                    await headerCmd.ExecuteNonQueryAsync();
                }

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0) throw new BusinessRuleException("Item quantity must be greater than zero.");
                    if (item.UnitPrice < 0) throw new BusinessRuleException("Unit price cannot be negative.");

                    // Generate next Detail ID
                    long detailId;
                    using (var dIdCmd = new OracleCommand("SELECT NVL(MAX(PurchaseDetailID), 0) + 1 FROM PurchaseDetails", conn))
                    {
                        dIdCmd.Transaction = tx;
                        detailId = Convert.ToInt64(await dIdCmd.ExecuteScalarAsync());
                    }

                    using (var itemCmd = new OracleCommand(
                        "INSERT INTO PurchaseDetails (PurchaseDetailID, PurchaseID, MedicineID, Quantity, UnitPrice, SubTotal) " +
                        "VALUES (:p_did, :p_pid, :p_mid, :p_qty, :p_price, NULL)", conn))
                    {
                        itemCmd.Transaction = tx;
                        itemCmd.Parameters.Add(new OracleParameter("p_did", detailId));
                        itemCmd.Parameters.Add(new OracleParameter("p_pid", purchaseId));
                        itemCmd.Parameters.Add(new OracleParameter("p_mid", item.MedicineID));
                        itemCmd.Parameters.Add(new OracleParameter("p_qty", item.Quantity));
                        itemCmd.Parameters.Add(new OracleParameter("p_price", item.UnitPrice));
                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            tx.Commit();
            return (await GetByIdAsync(purchaseId))!;
        }
        catch (OracleException ex)
        {
            tx.Rollback();
            if (ex.Number >= 20030 && ex.Number <= 20035)
                throw new BusinessRuleException(ex.Message.Split('\n')[0]);
            throw;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
