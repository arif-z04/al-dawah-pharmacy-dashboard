using System.Security.Claims;
using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Domain.Entities;
using AlDawahPharma.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace AlDawahPharma.Tests;

public class PasswordHasherTests
{
    private readonly ITestOutputHelper _output;

    public PasswordHasherTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateAndVerifyHashes()
    {
        var hasher = new PasswordHasher();
        string adminHash = hasher.HashPassword("admin123");
        string staffHash = hasher.HashPassword("rahim123");

        _output.WriteLine($"admin123: {adminHash}");
        _output.WriteLine($"rahim123: {staffHash}");

        Assert.True(hasher.VerifyPassword("admin123", adminHash));
        Assert.True(hasher.VerifyPassword("rahim123", staffHash));
        Assert.False(hasher.VerifyPassword("wrongPassword", adminHash));
    }
}

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtService;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Key", "AlDawahPharmaSecretSuperKey2026!MustBe32BytesLongMin"},
            {"Jwt:Issuer", "AlDawahPharmaApi"},
            {"Jwt:Audience", "AlDawahPharmaClient"},
            {"Jwt:ExpiryMinutes", "60"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtService = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_Should_Produce_Valid_Jwt_String()
    {
        var user = new User
        {
            UserID = 1,
            Username = "admin",
            FullName = "Admin User",
            Role = "Admin",
            Password = "hashedPassword"
        };

        var token = _jwtService.GenerateToken(user);

        Assert.NotNull(token);
        Assert.Contains(".", token);
        Assert.Equal(3, token.Split('.').Length); // Header.Payload.Signature
    }
}

public class ApiResponseTests
{
    [Fact]
    public void ApiResponse_Ok_Should_Set_Success_True()
    {
        var response = ApiResponse.Ok("TestData", "Operation completed.");

        Assert.True(response.Success);
        Assert.Equal("TestData", response.Data);
        Assert.Equal("Operation completed.", response.Message);
    }

    [Fact]
    public void ApiResponse_Fail_Should_Set_Success_False()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "MedicineID", new[] { "Medicine is required" } }
        };

        var response = ApiResponse.Fail("Validation failed", errors);

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("Validation failed", response.Message);
        Assert.NotNull(response.Errors);
        Assert.Single(response.Errors);
    }
}

public class DtoValidationTests
{
    [Fact]
    public void CreateStockAdjustmentRequest_Properties_Should_Hold_Values()
    {
        var req = new CreateStockAdjustmentRequest
        {
            MedicineID = 1,
            Quantity = 10,
            Reason = "Inventory Audit",
            Remarks = "Annual count"
        };

        Assert.Equal(1, req.MedicineID);
        Assert.Equal(10, req.Quantity);
        Assert.Equal("Inventory Audit", req.Reason);
        Assert.Equal("Annual count", req.Remarks);
    }

    [Fact]
    public void CreateSaleRequest_Should_Hold_Items_List()
    {
        var req = new CreateSaleRequest
        {
            CustomerID = 5,
            Items = new List<SaleItemRequest>
            {
                new() { MedicineID = 1, Quantity = 2, UnitPrice = 5.0m },
                new() { MedicineID = 2, Quantity = 1, UnitPrice = 12.5m }
            }
        };

        Assert.Equal(5, req.CustomerID);
        Assert.Equal(2, req.Items.Count);
        Assert.Equal(10.0m, req.Items[0].Quantity * req.Items[0].UnitPrice);
    }
}
