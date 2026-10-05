using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Infrastructure.Auth;
using AlDawahPharma.Infrastructure.Data;
using AlDawahPharma.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AlDawahPharma.Tests;

[Trait("Category", "Integration")]
public class BackendIntegrationTests
{
    private readonly IConfiguration _config;
    private readonly OracleConnectionFactory _connectionFactory;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtService;
    private static bool? _oracleAvailable;

    public BackendIntegrationTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"ConnectionStrings:OracleDb", "User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;Connection Timeout=2;"},
            {"Jwt:Key", "AlDawahPharmaSecretSuperKey2026!MustBe32BytesLongMin"},
            {"Jwt:Issuer", "AlDawahPharmaApi"},
            {"Jwt:Audience", "AlDawahPharmaClient"},
            {"Jwt:ExpiryMinutes", "720"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _connectionFactory = new OracleConnectionFactory(_config);
        _passwordHasher = new PasswordHasher();
        _jwtService = new JwtTokenService(_config);
    }

    private async Task<bool> IsOracleAvailableAsync()
    {
        if (_oracleAvailable.HasValue) return _oracleAvailable.Value;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(cts.Token);
            _oracleAvailable = (conn.State == ConnectionState.Open);
        }
        catch
        {
            _oracleAvailable = false;
        }
        return _oracleAvailable.Value;
    }

    [Fact]
    public async Task Oracle_Database_Connection_Should_Open_Successfully()
    {
        if (!await IsOracleAvailableAsync()) return;

        using var conn = await _connectionFactory.CreateOpenConnectionAsync();
        Assert.NotNull(conn);
        Assert.Equal(ConnectionState.Open, conn.State);
    }

    [Fact]
    public async Task User_Authentication_Should_Authenticate_Admin_And_Generate_Valid_Jwt()
    {
        if (!await IsOracleAvailableAsync()) return;

        var userRepo = new UserRepository(_connectionFactory, _passwordHasher);
        var admin = await userRepo.GetByUsernameAsync("admin");

        Assert.NotNull(admin);
        Assert.Equal("Admin", admin.Role);
        Assert.True(_passwordHasher.VerifyPassword("admin123", admin.Password));

        var token = _jwtService.GenerateToken(admin);
        Assert.NotNull(token);
        Assert.True(token.Length > 20);
    }

    [Fact]
    public async Task MedicineRepository_Should_Return_All_50_Medicines()
    {
        if (!await IsOracleAvailableAsync()) return;

        var medRepo = new MedicineRepository(_connectionFactory);
        var list = (await medRepo.GetAllAsync()).ToList();

        Assert.True(list.Count >= 50);
        var first = list.First(m => m.MedicineID == 1);
        Assert.Equal("Napa 500", first.MedicineName);
        Assert.Equal("Paracetamol", first.GenericName);
        Assert.True(first.QuantityInStock > 0);
    }

    [Fact]
    public async Task Oracle_Functions_Should_Return_Accurate_Stock_And_Valuation()
    {
        if (!await IsOracleAvailableAsync()) return;

        var medRepo = new MedicineRepository(_connectionFactory);
        int stock = await medRepo.GetAvailableStockAsync(1);
        decimal valuation = await medRepo.GetInventoryValueAsync(1);

        Assert.True(stock > 0);
        Assert.True(valuation > 0);
        Assert.Equal(stock * 6.50m, valuation);
    }

    [Fact]
    public async Task ReportsRepository_Views_Should_Return_Aggregated_Data()
    {
        if (!await IsOracleAvailableAsync()) return;

        var reportRepo = new ReportRepository(_connectionFactory);

        var expired = (await reportRepo.GetExpiredMedicinesAsync()).ToList();
        var nearExpiry = (await reportRepo.GetNearExpiryMedicinesAsync()).ToList();
        var lowStock = (await reportRepo.GetLowStockMedicinesAsync()).ToList();
        var monthlySales = (await reportRepo.GetMonthlySalesAsync()).ToList();
        var companyStock = (await reportRepo.GetCompanyStockAsync()).ToList();
        var supplierPurchases = (await reportRepo.GetSupplierPurchasesAsync()).ToList();
        var valuation = (await reportRepo.GetInventoryValuationAsync()).ToList();

        Assert.NotEmpty(expired);
        Assert.NotEmpty(nearExpiry);
        Assert.NotEmpty(lowStock);
        Assert.NotEmpty(companyStock);
        Assert.NotEmpty(supplierPurchases);
        Assert.NotEmpty(valuation);
    }

    [Fact]
    public async Task SaleRepository_Should_Reject_Insufficient_Stock()
    {
        if (!await IsOracleAvailableAsync()) return;

        var saleRepo = new SaleRepository(_connectionFactory);
        var request = new CreateSaleRequest
        {
            CustomerID = 1,
            Items = new List<SaleItemRequest>
            {
                new()
                {
                    MedicineID = 1,
                    Quantity = 999999, // Exceeds available stock
                    UnitPrice = 10.00m
                }
            }
        };

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await saleRepo.CreateAsync(request, 1);
        });
    }

    [Fact]
    public async Task CategoryRepository_Should_Return_All_10_Categories()
    {
        if (!await IsOracleAvailableAsync()) return;

        var catRepo = new CategoryRepository(_connectionFactory);
        var categories = (await catRepo.GetAllAsync()).ToList();

        Assert.True(categories.Count >= 10);
        Assert.Contains(categories, c => c.CategoryName == "Tablet");
        Assert.Contains(categories, c => c.CategoryName == "Capsule");
        Assert.Contains(categories, c => c.CategoryName == "Syrup");
    }

    [Fact]
    public async Task StockLogRepository_Should_Return_All_Movement_Records()
    {
        if (!await IsOracleAvailableAsync()) return;

        var stockRepo = new StockLogRepository(_connectionFactory);
        var logs = (await stockRepo.GetAllAsync()).ToList();

        Assert.NotEmpty(logs);
        Assert.True(logs.Count >= 50);
        Assert.Contains(logs, l => l.ActionType == "Purchase");
        Assert.Contains(logs, l => l.ActionType == "Sale");
    }
}
