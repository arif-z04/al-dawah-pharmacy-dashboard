using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public SaleRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<SaleDto>> GetAllAsync()
    {
        var list = new List<SaleDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "SELECT s.SaleID, s.CustomerID, c.CustomerName, s.UserID, u.FullName AS UserName, " +
            "s.SaleDate, s.TotalAmount " +
            "FROM Sales s " +
            "JOIN Customer c ON s.CustomerID = c.CustomerID " +
            "JOIN Users u ON s.UserID = u.UserID " +
            "ORDER BY s.SaleID DESC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SaleDto
            {
                SaleID = Convert.ToInt64(reader["SaleID"]),
                CustomerID = Convert.ToInt64(reader["CustomerID"]),
                CustomerName = reader["CustomerName"].ToString(),
                UserID = Convert.ToInt64(reader["UserID"]),
                UserName = reader["UserName"].ToString(),
                SaleDate = Convert.ToDateTime(reader["SaleDate"]),
                TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
            });
        }
        return list;
    }

    public async Task<SaleDto?> GetByIdAsync(long saleId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        SaleDto? sale = null;

        using (var cmd = new OracleCommand(
            "SELECT s.SaleID, s.CustomerID, c.CustomerName, s.UserID, u.FullName AS UserName, " +
            "s.SaleDate, s.TotalAmount " +
            "FROM Sales s " +
            "JOIN Customer c ON s.CustomerID = c.CustomerID " +
            "JOIN Users u ON s.UserID = u.UserID " +
            "WHERE s.SaleID = :p_id", conn))
        {
            cmd.Parameters.Add(new OracleParameter("p_id", saleId));
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                sale = new SaleDto
                {
                    SaleID = Convert.ToInt64(reader["SaleID"]),
                    CustomerID = Convert.ToInt64(reader["CustomerID"]),
                    CustomerName = reader["CustomerName"].ToString(),
                    UserID = Convert.ToInt64(reader["UserID"]),
                    UserName = reader["UserName"].ToString(),
                    SaleDate = Convert.ToDateTime(reader["SaleDate"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                };
            }
        }

        if (sale == null) return null;

        // Fetch detail items
        using (var detailCmd = new OracleCommand(
            "SELECT sd.SaleDetailID, sd.SaleID, sd.MedicineID, m.MedicineName, m.BatchNumber, " +
            "sd.Quantity, sd.UnitPrice, sd.SubTotal " +
            "FROM SalesDetails sd " +
            "JOIN Medicine m ON sd.MedicineID = m.MedicineID " +
            "WHERE sd.SaleID = :p_id " +
            "ORDER BY sd.SaleDetailID ASC", conn))
        {
            detailCmd.Parameters.Add(new OracleParameter("p_id", saleId));
            using var reader = await detailCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sale.Details.Add(new SaleDetailDto
                {
                    SaleDetailID = Convert.ToInt64(reader["SaleDetailID"]),
                    SaleID = Convert.ToInt64(reader["SaleID"]),
                    MedicineID = Convert.ToInt64(reader["MedicineID"]),
                    MedicineName = reader["MedicineName"].ToString(),
                    BatchNumber = reader["BatchNumber"].ToString(),
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                    SubTotal = Convert.ToDecimal(reader["SubTotal"])
                });
            }
        }

        return sale;
    }

    public async Task<SaleDto> CreateAsync(CreateSaleRequest request, long userId)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("Sale invoice must contain at least one medicine item.");

        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            // Verify Customer exists
            using (var custCmd = new OracleCommand("SELECT COUNT(*) FROM Customer WHERE CustomerID = :p_cust", conn))
            {
                custCmd.Transaction = tx;
                custCmd.Parameters.Add(new OracleParameter("p_cust", request.CustomerID));
                if (Convert.ToInt32(await custCmd.ExecuteScalarAsync()) == 0)
                    throw new BusinessRuleException($"Customer with ID {request.CustomerID} does not exist.");
            }

            // Verify User exists
            using (var userCmd = new OracleCommand("SELECT COUNT(*) FROM Users WHERE UserID = :p_usr", conn))
            {
                userCmd.Transaction = tx;
                userCmd.Parameters.Add(new OracleParameter("p_usr", userId));
                if (Convert.ToInt32(await userCmd.ExecuteScalarAsync()) == 0)
                    throw new BusinessRuleException($"User with ID {userId} does not exist.");
            }

            // Generate next manual SaleID
            long saleId;
            using (var idCmd = new OracleCommand("SELECT NVL(MAX(SaleID), 0) + 1 FROM Sales", conn))
            {
                idCmd.Transaction = tx;
                saleId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
            }

            // 1. If exactly one item is present, call Oracle Stored Procedure RECORD_SALE
            if (request.Items.Count == 1)
            {
                var item = request.Items[0];
                if (item.Quantity <= 0) throw new BusinessRuleException("Sale quantity must be greater than zero.");
                if (item.UnitPrice < 0) throw new BusinessRuleException("Unit selling price cannot be negative.");

                using var procCmd = new OracleCommand("RECORD_SALE", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    Transaction = tx
                };
                procCmd.Parameters.Add(new OracleParameter("p_saleid", OracleDbType.Int64, saleId, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_customerid", OracleDbType.Int64, request.CustomerID, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_userid", OracleDbType.Int64, userId, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_medicineid", OracleDbType.Int64, item.MedicineID, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_quantity", OracleDbType.Int32, item.Quantity, ParameterDirection.Input));
                procCmd.Parameters.Add(new OracleParameter("p_unitprice", OracleDbType.Decimal, item.UnitPrice, ParameterDirection.Input));

                await procCmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Multi-item sale: Insert header then insert detail rows
                // Triggers automatically enforce stock sufficiency, compute SubTotal, decrease stock, and update TotalAmount!
                using (var headerCmd = new OracleCommand(
                    "INSERT INTO Sales (SaleID, CustomerID, UserID, SaleDate, TotalAmount) " +
                    "VALUES (:p_id, :p_cust, :p_usr, SYSDATE, 0)", conn))
                {
                    headerCmd.Transaction = tx;
                    headerCmd.Parameters.Add(new OracleParameter("p_id", saleId));
                    headerCmd.Parameters.Add(new OracleParameter("p_cust", request.CustomerID));
                    headerCmd.Parameters.Add(new OracleParameter("p_usr", userId));
                    await headerCmd.ExecuteNonQueryAsync();
                }

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0) throw new BusinessRuleException("Item quantity must be greater than zero.");
                    if (item.UnitPrice < 0) throw new BusinessRuleException("Unit price cannot be negative.");

                    // Generate next Detail ID
                    long detailId;
                    using (var dIdCmd = new OracleCommand("SELECT NVL(MAX(SaleDetailID), 0) + 1 FROM SalesDetails", conn))
                    {
                        dIdCmd.Transaction = tx;
                        detailId = Convert.ToInt64(await dIdCmd.ExecuteScalarAsync());
                    }

                    using (var itemCmd = new OracleCommand(
                        "INSERT INTO SalesDetails (SaleDetailID, SaleID, MedicineID, Quantity, UnitPrice, SubTotal) " +
                        "VALUES (:p_did, :p_sid, :p_mid, :p_qty, :p_price, NULL)", conn))
                    {
                        itemCmd.Transaction = tx;
                        itemCmd.Parameters.Add(new OracleParameter("p_did", detailId));
                        itemCmd.Parameters.Add(new OracleParameter("p_sid", saleId));
                        itemCmd.Parameters.Add(new OracleParameter("p_mid", item.MedicineID));
                        itemCmd.Parameters.Add(new OracleParameter("p_qty", item.Quantity));
                        itemCmd.Parameters.Add(new OracleParameter("p_price", item.UnitPrice));
                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }
            }

            tx.Commit();
            return (await GetByIdAsync(saleId))!;
        }
        catch (OracleException ex)
        {
            tx.Rollback();
            // Handle ORA-20001 (Insufficient stock trigger error)
            if (ex.Number == 20001)
                throw new BusinessRuleException(ex.Message.Split('\n')[0]);
            if (ex.Number >= 20040 && ex.Number <= 20045)
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
