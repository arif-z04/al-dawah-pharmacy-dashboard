using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public ReportRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ExpiredMedicineReportDto>> GetExpiredMedicinesAsync()
    {
        var list = new List<ExpiredMedicineReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query ExpiredMedicine_View enriched with Category & Company
        var sql = "SELECT v.MedicineID, v.MedicineName, v.GenericName, c.CategoryName, cp.CompanyName, " +
                  "v.BatchNumber, v.QuantityInStock, m.PurchasePrice, v.SellingPrice, v.ExpiryDate, " +
                  "ROUND(SYSDATE - v.ExpiryDate) AS DaysExpired " +
                  "FROM ExpiredMedicine_View v " +
                  "JOIN Medicine m ON v.MedicineID = m.MedicineID " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "ORDER BY v.ExpiryDate ASC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ExpiredMedicineReportDto
            {
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString()!,
                GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
                CategoryName = reader["CategoryName"].ToString(),
                CompanyName = reader["CompanyName"].ToString(),
                BatchNumber = reader["BatchNumber"].ToString()!,
                QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
                PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
                ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                DaysExpired = Convert.ToInt32(reader["DaysExpired"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<NearExpiryReportDto>> GetNearExpiryMedicinesAsync()
    {
        var list = new List<NearExpiryReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query NearExpiryMedicine_View
        var sql = "SELECT v.MedicineID, v.MedicineName, v.GenericName, c.CategoryName, cp.CompanyName, " +
                  "v.BatchNumber, v.QuantityInStock, v.SellingPrice, v.ExpiryDate, " +
                  "ROUND(v.ExpiryDate - SYSDATE) AS DaysRemaining " +
                  "FROM NearExpiryMedicine_View v " +
                  "JOIN Medicine m ON v.MedicineID = m.MedicineID " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "ORDER BY v.ExpiryDate ASC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new NearExpiryReportDto
            {
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString()!,
                GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
                CategoryName = reader["CategoryName"].ToString(),
                CompanyName = reader["CompanyName"].ToString(),
                BatchNumber = reader["BatchNumber"].ToString()!,
                QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
                SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
                ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                DaysRemaining = Convert.ToInt32(reader["DaysRemaining"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<LowStockReportDto>> GetLowStockMedicinesAsync()
    {
        var list = new List<LowStockReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query LowStock_View
        var sql = "SELECT v.MedicineID, v.MedicineName, v.GenericName, c.CategoryName, cp.CompanyName, s.SupplierName, " +
                  "v.QuantityInStock, v.ReorderLevel, (v.ReorderLevel - v.QuantityInStock) AS StockDeficit, v.SellingPrice " +
                  "FROM LowStock_View v " +
                  "JOIN Medicine m ON v.MedicineID = m.MedicineID " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "JOIN Supplier s ON m.SupplierID = s.SupplierID " +
                  "ORDER BY StockDeficit DESC, v.QuantityInStock ASC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LowStockReportDto
            {
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString()!,
                GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
                CategoryName = reader["CategoryName"].ToString(),
                CompanyName = reader["CompanyName"].ToString(),
                SupplierName = reader["SupplierName"].ToString(),
                QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
                ReorderLevel = Convert.ToInt32(reader["ReorderLevel"]),
                StockDeficit = Convert.ToInt32(reader["StockDeficit"]),
                SellingPrice = Convert.ToDecimal(reader["SellingPrice"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<MonthlySalesReportDto>> GetMonthlySalesAsync()
    {
        var list = new List<MonthlySalesReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query MonthlySales_View joined with line items
        var sql = "SELECT TO_CHAR(s.SaleDate, 'YYYY-MM') AS SalesMonth, " +
                  "COUNT(DISTINCT s.SaleID) AS TotalInvoices, " +
                  "NVL(SUM(sd.Quantity), 0) AS TotalUnitsSold, " +
                  "NVL(SUM(s.TotalAmount), 0) AS TotalRevenue " +
                  "FROM Sales s " +
                  "LEFT JOIN SalesDetails sd ON s.SaleID = sd.SaleID " +
                  "GROUP BY TO_CHAR(s.SaleDate, 'YYYY-MM') " +
                  "ORDER BY SalesMonth DESC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new MonthlySalesReportDto
            {
                SalesMonth = reader["SalesMonth"].ToString()!,
                TotalInvoices = Convert.ToInt32(reader["TotalInvoices"]),
                TotalUnitsSold = Convert.ToInt32(reader["TotalUnitsSold"]),
                TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<TopSellingMedicineDto>> GetTopSellingMedicinesAsync(int top = 10)
    {
        var list = new List<TopSellingMedicineDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        var sql = "SELECT * FROM ( " +
                  "SELECT m.MedicineID, m.MedicineName, m.GenericName, c.CategoryName, cp.CompanyName, " +
                  "SUM(sd.Quantity) AS TotalQuantitySold, SUM(sd.SubTotal) AS TotalRevenueGenerated " +
                  "FROM SalesDetails sd " +
                  "JOIN Medicine m ON sd.MedicineID = m.MedicineID " +
                  "JOIN Category c ON m.CategoryID = c.CategoryID " +
                  "JOIN Company cp ON m.CompanyID = cp.CompanyID " +
                  "GROUP BY m.MedicineID, m.MedicineName, m.GenericName, c.CategoryName, cp.CompanyName " +
                  "ORDER BY TotalQuantitySold DESC " +
                  ") WHERE ROWNUM <= :p_top";

        using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("p_top", top));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TopSellingMedicineDto
            {
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString()!,
                GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
                CategoryName = reader["CategoryName"].ToString(),
                CompanyName = reader["CompanyName"].ToString(),
                TotalQuantitySold = Convert.ToInt32(reader["TotalQuantitySold"]),
                TotalRevenueGenerated = Convert.ToDecimal(reader["TotalRevenueGenerated"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<CompanyStockReportDto>> GetCompanyStockAsync()
    {
        var list = new List<CompanyStockReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query CompanyWiseStock_View with Inventory Value
        var sql = "SELECT cp.CompanyID, cp.CompanyName, " +
                  "COUNT(m.MedicineID) AS TotalMedicineItems, " +
                  "NVL(SUM(m.QuantityInStock), 0) AS TotalStockUnits, " +
                  "NVL(SUM(m.QuantityInStock * m.PurchasePrice), 0) AS TotalInventoryValue " +
                  "FROM Company cp " +
                  "LEFT JOIN Medicine m ON cp.CompanyID = m.CompanyID " +
                  "GROUP BY cp.CompanyID, cp.CompanyName " +
                  "ORDER BY TotalInventoryValue DESC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CompanyStockReportDto
            {
                CompanyID = Convert.ToInt64(reader["CompanyID"]),
                CompanyName = reader["CompanyName"].ToString()!,
                TotalMedicineItems = Convert.ToInt32(reader["TotalMedicineItems"]),
                TotalStockUnits = Convert.ToInt32(reader["TotalStockUnits"]),
                TotalInventoryValue = Convert.ToDecimal(reader["TotalInventoryValue"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<SupplierPurchaseReportDto>> GetSupplierPurchasesAsync()
    {
        var list = new List<SupplierPurchaseReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query SupplierWisePurchase_View
        var sql = "SELECT s.SupplierID, s.SupplierName, " +
                  "COUNT(DISTINCT p.PurchaseID) AS TotalPurchaseOrders, " +
                  "NVL(SUM(pd.Quantity), 0) AS TotalQuantityPurchased, " +
                  "NVL(SUM(pd.SubTotal), 0) AS TotalPurchaseExpenditure " +
                  "FROM Supplier s " +
                  "LEFT JOIN Purchase p ON s.SupplierID = p.SupplierID " +
                  "LEFT JOIN PurchaseDetails pd ON p.PurchaseID = pd.PurchaseID " +
                  "GROUP BY s.SupplierID, s.SupplierName " +
                  "ORDER BY TotalPurchaseExpenditure DESC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SupplierPurchaseReportDto
            {
                SupplierID = Convert.ToInt64(reader["SupplierID"]),
                SupplierName = reader["SupplierName"].ToString()!,
                TotalPurchaseOrders = Convert.ToInt32(reader["TotalPurchaseOrders"]),
                TotalQuantityPurchased = Convert.ToInt32(reader["TotalQuantityPurchased"]),
                TotalPurchaseExpenditure = Convert.ToDecimal(reader["TotalPurchaseExpenditure"])
            });
        }
        return list;
    }

    public async Task<IEnumerable<InventoryValuationReportDto>> GetInventoryValuationAsync()
    {
        var list = new List<InventoryValuationReportDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Query InventoryValue_View with details
        var sql = "SELECT m.MedicineID, m.MedicineName, m.GenericName, m.BatchNumber, " +
                  "m.QuantityInStock, m.PurchasePrice, " +
                  "(m.QuantityInStock * m.PurchasePrice) AS InventoryValuationAtCost, " +
                  "m.SellingPrice, (m.QuantityInStock * m.SellingPrice) AS ProjectedRevenueAtRetail, " +
                  "((m.QuantityInStock * m.SellingPrice) - (m.QuantityInStock * m.PurchasePrice)) AS ProjectedGrossMargin " +
                  "FROM Medicine m " +
                  "ORDER BY InventoryValuationAtCost DESC";

        using var cmd = new OracleCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new InventoryValuationReportDto
            {
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString()!,
                GenericName = reader["GenericName"] == DBNull.Value ? null : reader["GenericName"].ToString(),
                BatchNumber = reader["BatchNumber"].ToString()!,
                QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
                PurchasePrice = Convert.ToDecimal(reader["PurchasePrice"]),
                InventoryValuationAtCost = Convert.ToDecimal(reader["InventoryValuationAtCost"]),
                SellingPrice = Convert.ToDecimal(reader["SellingPrice"]),
                ProjectedRevenueAtRetail = Convert.ToDecimal(reader["ProjectedRevenueAtRetail"]),
                ProjectedGrossMargin = Convert.ToDecimal(reader["ProjectedGrossMargin"])
            });
        }
        return list;
    }
}
