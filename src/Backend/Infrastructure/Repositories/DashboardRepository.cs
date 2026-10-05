using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly IReportRepository _reportRepository;

    public DashboardRepository(IOracleConnectionFactory connectionFactory, IReportRepository reportRepository)
    {
        _connectionFactory = connectionFactory;
        _reportRepository = reportRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var summary = new DashboardSummaryDto();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM Medicine) AS TotalMedicines,
                (SELECT COUNT(*) FROM Category) AS TotalCategories,
                (SELECT COUNT(*) FROM Company) AS TotalCompanies,
                (SELECT COUNT(*) FROM Supplier) AS TotalSuppliers,
                (SELECT COUNT(*) FROM Customer) AS TotalCustomers,
                (SELECT COUNT(*) FROM LowStock_View) AS LowStockCount,
                (SELECT COUNT(*) FROM ExpiredMedicine_View) AS ExpiredCount,
                (SELECT COUNT(*) FROM NearExpiryMedicine_View) AS NearExpiryCount,
                (SELECT NVL(SUM(QuantityInStock * PurchasePrice), 0) FROM Medicine) AS TotalInventoryValue,
                (SELECT NVL(SUM(TotalAmount), 0) FROM Sales) AS TotalSalesRevenue,
                (SELECT NVL(SUM(TotalAmount), 0) FROM Purchase) AS TotalPurchasesExpenditure,
                (SELECT COUNT(*) FROM Sales) AS TotalSalesCount,
                (SELECT COUNT(*) FROM Purchase) AS TotalPurchasesCount
            FROM DUAL";

        using (var cmd = new OracleCommand(sql, conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                summary.TotalMedicines = Convert.ToInt32(reader["TotalMedicines"]);
                summary.TotalCategories = Convert.ToInt32(reader["TotalCategories"]);
                summary.TotalCompanies = Convert.ToInt32(reader["TotalCompanies"]);
                summary.TotalSuppliers = Convert.ToInt32(reader["TotalSuppliers"]);
                summary.TotalCustomers = Convert.ToInt32(reader["TotalCustomers"]);
                summary.LowStockCount = Convert.ToInt32(reader["LowStockCount"]);
                summary.ExpiredCount = Convert.ToInt32(reader["ExpiredCount"]);
                summary.NearExpiryCount = Convert.ToInt32(reader["NearExpiryCount"]);
                summary.TotalInventoryValue = Convert.ToDecimal(reader["TotalInventoryValue"]);
                summary.TotalSalesRevenue = Convert.ToDecimal(reader["TotalSalesRevenue"]);
                summary.TotalPurchasesExpenditure = Convert.ToDecimal(reader["TotalPurchasesExpenditure"]);
                summary.TotalSalesCount = Convert.ToInt32(reader["TotalSalesCount"]);
                summary.TotalPurchasesCount = Convert.ToInt32(reader["TotalPurchasesCount"]);
            }
        }

        // Fetch monthly sales chart data
        var sales = await _reportRepository.GetMonthlySalesAsync();
        summary.RecentSalesChart = sales.Take(6).ToList();

        // Fetch top selling medicines
        var topMed = await _reportRepository.GetTopSellingMedicinesAsync(5);
        summary.TopSellingMedicines = topMed.ToList();

        return summary;
    }
}
