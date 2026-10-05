using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public MedicineRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<MedicineDto>> GetAllAsync(string? searchTerm = null, long? categoryId = null, long? companyId = null)
    {
        var list = new List<MedicineDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        var sql = "SELECT m.MedicineID, m.MedicineName, m.GenericName, m.CategoryID, c.CategoryName, " +
                  "m.CompanyID, cp.CompanyName, m.SupplierID, s.SupplierName, m.BatchNumber, " +
                  "m.PurchasePrice, m.SellingPrice, m.QuantityInStock, m.ReorderLevel, " +
                  "m.ManufacturingDate, m.ExpiryDate, m.Barcode, m.Description, m.CreatedAt " +
                  "FROM Medicine m " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "JOIN Supplier s ON m.SupplierID = s.SupplierID " +
                  "WHERE 1=1 ";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += "AND (UPPER(m.MedicineName) LIKE :p_search OR UPPER(m.GenericName) LIKE :p_search OR UPPER(m.BatchNumber) LIKE :p_search OR m.Barcode LIKE :p_search) ";
        }
        if (categoryId.HasValue)
        {
            sql += "AND m.CategoryID = :p_catId ";
        }
        if (companyId.HasValue)
        {
            sql += "AND m.CompanyID = :p_compId ";
        }

        sql += "ORDER BY m.MedicineID ASC";

        using var cmd = new OracleCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            cmd.Parameters.Add(new OracleParameter("p_search", $"%{searchTerm.Trim().ToUpper()}%"));
        }
        if (categoryId.HasValue)
        {
            cmd.Parameters.Add(new OracleParameter("p_catId", categoryId.Value));
        }
        if (companyId.HasValue)
        {
            cmd.Parameters.Add(new OracleParameter("p_compId", companyId.Value));
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapMedicine(reader));
        }
        return list;
    }

    public async Task<MedicineDto?> GetByIdAsync(long medicineId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        var sql = "SELECT m.MedicineID, m.MedicineName, m.GenericName, m.CategoryID, c.CategoryName, " +
                  "m.CompanyID, cp.CompanyName, m.SupplierID, s.SupplierName, m.BatchNumber, " +
                  "m.PurchasePrice, m.SellingPrice, m.QuantityInStock, m.ReorderLevel, " +
                  "m.ManufacturingDate, m.ExpiryDate, m.Barcode, m.Description, m.CreatedAt " +
                  "FROM Medicine m " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "JOIN Supplier s ON m.SupplierID = s.SupplierID " +
                  "WHERE m.MedicineID = :p_id";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("p_id", medicineId));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapMedicine(reader);
        }
        return null;
    }

    public async Task<MedicineDto?> GetByBatchNumberAsync(string batchNumber)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        var sql = "SELECT m.MedicineID, m.MedicineName, m.GenericName, m.CategoryID, c.CategoryName, " +
                  "m.CompanyID, cp.CompanyName, m.SupplierID, s.SupplierName, m.BatchNumber, " +
                  "m.PurchasePrice, m.SellingPrice, m.QuantityInStock, m.ReorderLevel, " +
                  "m.ManufacturingDate, m.ExpiryDate, m.Barcode, m.Description, m.CreatedAt " +
                  "FROM Medicine m " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "JOIN Supplier s ON m.SupplierID = s.SupplierID " +
                  "WHERE UPPER(m.BatchNumber) = UPPER(:p_batch)";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("p_batch", batchNumber.Trim()));

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapMedicine(reader);
        }
        return null;
    }

    public async Task<MedicineDto> CreateAsync(CreateMedicineRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        // Generate manual ID respecting DB strategy
        long nextId;
        using (var idCmd = new OracleCommand("SELECT NVL(MAX(MedicineID), 0) + 1 FROM Medicine", conn))
        {
            nextId = Convert.ToInt64(await idCmd.ExecuteScalarAsync());
        }

        // Call Oracle Stored Procedure ADD_MEDICINE
        try
        {
            using var procCmd = new OracleCommand("ADD_MEDICINE", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            procCmd.Parameters.Add(new OracleParameter("p_medicineid", OracleDbType.Int64, nextId, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_medicinename", OracleDbType.Varchar2, request.MedicineName.Trim(), ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_genericname", OracleDbType.Varchar2, (object?)request.GenericName?.Trim() ?? DBNull.Value, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_categoryid", OracleDbType.Int64, request.CategoryID, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_companyid", OracleDbType.Int64, request.CompanyID, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_supplierid", OracleDbType.Int64, request.SupplierID, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_batchnumber", OracleDbType.Varchar2, request.BatchNumber.Trim(), ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_purchaseprice", OracleDbType.Decimal, request.PurchasePrice, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_sellingprice", OracleDbType.Decimal, request.SellingPrice, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_quantityinstock", OracleDbType.Int32, request.QuantityInStock, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_reorderlevel", OracleDbType.Int32, request.ReorderLevel, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_manufacturingdate", OracleDbType.Date, request.ManufacturingDate, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_expirydate", OracleDbType.Date, request.ExpiryDate, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_barcode", OracleDbType.Varchar2, (object?)request.Barcode?.Trim() ?? DBNull.Value, ParameterDirection.Input));
            procCmd.Parameters.Add(new OracleParameter("p_description", OracleDbType.Varchar2, (object?)request.Description?.Trim() ?? DBNull.Value, ParameterDirection.Input));

            await procCmd.ExecuteNonQueryAsync();
        }
        catch (OracleException ex)
        {
            if (ex.Number >= 20020 && ex.Number <= 20027 || ex.Number == 1)
                throw new BusinessRuleException(ex.Message.Split('\n')[0]);
            throw;
        }

        return (await GetByIdAsync(nextId))!;
    }

    public async Task<bool> UpdateAsync(long medicineId, UpdateMedicineRequest request)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        var sql = "UPDATE Medicine SET " +
                  "MedicineName = :p_name, GenericName = :p_generic, CategoryID = :p_cat, CompanyID = :p_comp, " +
                  "SupplierID = :p_sup, BatchNumber = :p_batch, PurchasePrice = :p_pprice, SellingPrice = :p_sprice, " +
                  "ReorderLevel = :p_reorder, ManufacturingDate = :p_mfg, ExpiryDate = :p_exp, Barcode = :p_barcode, " +
                  "Description = :p_desc WHERE MedicineID = :p_id";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("p_name", request.MedicineName.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_generic", (object?)request.GenericName?.Trim() ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_cat", request.CategoryID));
        cmd.Parameters.Add(new OracleParameter("p_comp", request.CompanyID));
        cmd.Parameters.Add(new OracleParameter("p_sup", request.SupplierID));
        cmd.Parameters.Add(new OracleParameter("p_batch", request.BatchNumber.Trim()));
        cmd.Parameters.Add(new OracleParameter("p_pprice", request.PurchasePrice));
        cmd.Parameters.Add(new OracleParameter("p_sprice", request.SellingPrice));
        cmd.Parameters.Add(new OracleParameter("p_reorder", request.ReorderLevel));
        cmd.Parameters.Add(new OracleParameter("p_mfg", request.ManufacturingDate));
        cmd.Parameters.Add(new OracleParameter("p_exp", request.ExpiryDate));
        cmd.Parameters.Add(new OracleParameter("p_barcode", (object?)request.Barcode?.Trim() ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_desc", (object?)request.Description?.Trim() ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("p_id", medicineId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long medicineId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand("DELETE FROM Medicine WHERE MedicineID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", medicineId));

        try
        {
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch (OracleException ex) when (ex.Number == 2292)
        {
            throw new BusinessRuleException("Cannot delete medicine because transaction records (purchases/sales/stock logs) exist for it.");
        }
    }

    public async Task<int> GetAvailableStockAsync(long medicineId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Invoke Oracle Function GET_AVAILABLE_STOCK
        using var cmd = new OracleCommand("SELECT GET_AVAILABLE_STOCK(:p_id) FROM DUAL", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", medicineId));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<decimal> GetInventoryValueAsync(long medicineId)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Invoke Oracle Function GET_INVENTORY_VALUE
        using var cmd = new OracleCommand("SELECT GET_INVENTORY_VALUE(:p_id) FROM DUAL", conn);
        cmd.Parameters.Add(new OracleParameter("p_id", medicineId));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }

    private static MedicineDto MapMedicine(IDataRecord reader)
    {
        return new MedicineDto
        {
            MedicineID = Convert.ToInt64(reader["MedicineID"]),
            MedicineName = reader["MedicineName"].ToString()!,
            GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
            CategoryID = Convert.ToInt64(reader["CategoryID"]),
            CategoryName = reader["CategoryName"].ToString(),
            CompanyID = Convert.ToInt64(reader["CompanyID"]),
            CompanyName = reader["CompanyName"].ToString(),
            SupplierID = Convert.ToInt64(reader["SupplierID"]),
            SupplierName = reader["SupplierName"].ToString(),
            BatchNumber = reader["BatchNumber"].ToString()!,
            PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
            SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
            QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
            ReorderLevel = Convert.ToInt32(reader["ReorderLevel"]),
            ManufacturingDate = Convert.ToDateTime(reader["ManufacturingDate"]),
            ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
            Barcode = reader["Barcode"] == DBNull.Value ? null : reader["Barcode"].ToString(),
            Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
        };
    }
}
