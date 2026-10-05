# Chapter 4: Backend C# .NET Web API Architecture & Code Walkthrough

In this chapter, we explore the implementation of the backend application: a modern, high-performance **ASP.NET Core 10 Web API** written in C#.

We will walk through the architecture of the .NET solution, dependency injection, middleware pipelines, data transfer objects (DTOs), ADO.NET and Dapper data access with Oracle, security hashing, JWT generation, centralized exception handling, and controller endpoints.

---

## 1. Solution Architecture & Project Dependencies

The solution is divided into four cleanly decoupled projects located under `src/Backend/`:

```
AlDawahPharma/
├── src/Backend/
│   ├── Domain/                 # Pure domain entities (Users, Medicine, Sales, Purchase, StockLog, etc.)
│   ├── Application/            # DTOs, Repository interfaces, business exceptions, ApiResponse envelope
│   ├── Infrastructure/         # Oracle Database access, Dapper/ADO.NET, BCrypt, JWT service
│   └── Api/                    # REST API Controllers, ExceptionHandlingMiddleware, Program.cs
└── tests/
    └── AlDawahPharma.Tests/    # Automated xUnit integration and unit tests
```

### Dependency Graph & Clean Architecture Rules
- **Domain**: Holds pure C# business models. It has zero dependencies on ASP.NET, Entity Framework, Oracle, or external NuGet packages.
- **Application**: Defines business contracts (interfaces like `IMedicineRepository`, `ISaleRepository`), DTOs, and custom domain exceptions (`NotFoundException`, `ValidationException`).
- **Infrastructure**: Implements the contracts defined in Application using `Oracle.ManagedDataAccess.Core`, Dapper, and `BCrypt.Net-Next`.
- **Api**: The entry point hosting the HTTP pipeline, controllers, middleware, and CORS configuration.

---

## 2. Server Startup & Middleware Pipeline: `Program.cs`

`Program.cs` configures the web host, registers services in the **Dependency Injection (DI) Container**, and builds the HTTP request-response pipeline.

```csharp
using System.Text;
using AlDawahPharma.Api.Middleware;
using AlDawahPharma.Application.Interfaces;
using AlDawahPharma.Infrastructure.Data;
using AlDawahPharma.Infrastructure.Repositories;
using AlDawahPharma.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection Configuration
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. JWT Bearer Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AlDawahPharmaSecretKeySuperSecure2026!#*";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AlDawahPharmaApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AlDawahPharmaClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Strict expiration without 5-minute grace period
        };
    });

// 3. Infrastructure & Repository Registrations
builder.Services.AddSingleton<IOracleConnectionFactory, OracleConnectionFactory>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IStockLogRepository, StockLogRepository>();
builder.Services.AddScoped<IExpiryAlertRepository, ExpiryAlertRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

var app = builder.Build();

// 4. HTTP Middleware Pipeline Execution Order
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Centralized Exception Interceptor
app.UseCors("AllowAll");

// Serve Vanilla JS Frontend as static files directly from ASP.NET Core
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## 3. Centralized Exception Handling: `ExceptionHandlingMiddleware.cs`

In production, raw database exceptions or unhandled stack traces must never be exposed to clients. `ExceptionHandlingMiddleware.cs` intercepts all exceptions across the entire request lifecycle and translates them into uniform, sanitized JSON responses.

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected server error occurred.";
        IDictionary<string, string[]>? validationErrors = null;

        switch (exception)
        {
            case NotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                message = notFoundEx.Message;
                break;

            case ValidationException valEx:
                statusCode = HttpStatusCode.BadRequest;
                message = valEx.Message;
                validationErrors = valEx.Errors;
                break;

            case BusinessRuleException bizEx:
                statusCode = HttpStatusCode.BadRequest;
                message = bizEx.Message;
                break;

            case UnauthorizedException unauthEx:
                statusCode = HttpStatusCode.Unauthorized;
                message = unauthEx.Message;
                break;

            case ForbiddenException forbEx:
                statusCode = HttpStatusCode.Forbidden;
                message = forbEx.Message;
                break;

            case OracleException oraEx:
                statusCode = HttpStatusCode.BadRequest;
                message = ParseOracleError(oraEx);
                _logger.LogError(oraEx, "Oracle Error [{ErrorNumber}]: {OracleMessage}", oraEx.Number, oraEx.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
                message = "An internal server error occurred. Please contact the administrator.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, validationErrors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string ParseOracleError(OracleException ex)
    {
        return ex.Number switch
        {
            20001 => CleanOracleMessage(ex.Message), // Insufficient stock
            20010 or 20011 => CleanOracleMessage(ex.Message), // Category duplicate
            >= 20020 and <= 20027 => CleanOracleMessage(ex.Message), // Medicine rules
            >= 20030 and <= 20035 => CleanOracleMessage(ex.Message), // Purchase rules
            >= 20040 and <= 20045 => CleanOracleMessage(ex.Message), // Sale rules
            1 => "A record with this unique identifier or code already exists.",
            2291 => "Foreign key violation: The referenced record does not exist.",
            2292 => "Cannot perform operation because dependent related records exist.",
            _ => "A database constraint or validation rule was violated."
        };
    }
}
```

---

## 4. Repositories: Dapper & ADO.NET Data Access

### Connection Factory: `OracleConnectionFactory.cs`
Manages Oracle database connections and connection pooling efficiently:
```csharp
public class OracleConnectionFactory : IOracleConnectionFactory
{
    private readonly string _connectionString;

    public OracleConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleDb")
            ?? "Data Source=localhost:1521/FREE;User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;";
    }

    public IDbConnection CreateConnection()
    {
        return new OracleConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
```

### Sale Repository: `SaleRepository.cs`
Handles sales transactions, calling Oracle stored procedures and retrieving itemized invoices:
```csharp
public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, long userId)
{
    using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
    using var transaction = conn.BeginTransaction();

    try
    {
        // 1. Generate next SaleID
        long saleId;
        using (var seqCmd = new OracleCommand("SELECT NVL(MAX(SaleID), 0) + 1 FROM Sales", conn))
        {
            seqCmd.Transaction = transaction;
            saleId = Convert.ToInt64(await seqCmd.ExecuteScalarAsync());
        }

        // 2. Insert master Sales record
        using (var saleCmd = new OracleCommand(
            "INSERT INTO Sales (SaleID, CustomerID, UserID, SaleDate, TotalAmount) " +
            "VALUES (:p_saleid, :p_customerid, :p_userid, SYSDATE, 0)", conn))
        {
            saleCmd.Transaction = transaction;
            saleCmd.Parameters.Add(new OracleParameter("p_saleid", saleId));
            saleCmd.Parameters.Add(new OracleParameter("p_customerid", request.CustomerID));
            saleCmd.Parameters.Add(new OracleParameter("p_userid", userId));
            await saleCmd.ExecuteNonQueryAsync();
        }

        // 3. Insert each child line item into SalesDetails
        long detailId;
        using (var dSeqCmd = new OracleCommand("SELECT NVL(MAX(SaleDetailID), 0) FROM SalesDetails", conn))
        {
            dSeqCmd.Transaction = transaction;
            detailId = Convert.ToInt64(await dSeqCmd.ExecuteScalarAsync());
        }

        foreach (var item in request.Items)
        {
            detailId++;
            using var dCmd = new OracleCommand(
                "INSERT INTO SalesDetails (SaleDetailID, SaleID, MedicineID, Quantity, UnitPrice, SubTotal) " +
                "VALUES (:p_did, :p_sid, :p_mid, :p_qty, :p_price, :p_sub)", conn);
            dCmd.Transaction = transaction;
            dCmd.Parameters.Add(new OracleParameter("p_did", detailId));
            dCmd.Parameters.Add(new OracleParameter("p_sid", saleId));
            dCmd.Parameters.Add(new OracleParameter("p_mid", item.MedicineID));
            dCmd.Parameters.Add(new OracleParameter("p_qty", item.Quantity));
            dCmd.Parameters.Add(new OracleParameter("p_price", item.UnitPrice));
            dCmd.Parameters.Add(new OracleParameter("p_sub", item.Quantity * item.UnitPrice));
            await dCmd.ExecuteNonQueryAsync();
        }

        transaction.Commit();
        return (await GetByIdAsync(saleId))!;
    }
    catch
    {
        transaction.Rollback();
        raise;
    }
}
```

---

## 5. REST API Controllers

### `StockController.cs`: Routing & Auditing
Exposes endpoints for viewing stock logs and performing inventory adjustments:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockLogRepository _stockLogRepository;

    public StockController(IStockLogRepository stockLogRepository)
    {
        _stockLogRepository = stockLogRepository;
    }

    [HttpGet]
    [HttpGet("log")]
    [HttpGet("/api/stock-log")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockLogDto>>>> GetLogs(
        [FromQuery] long? medicineId,
        [FromQuery] string? actionType)
    {
        var list = await _stockLogRepository.GetAllAsync(medicineId, actionType);
        return Ok(ApiResponse<IEnumerable<StockLogDto>>.Ok(list));
    }

    [HttpPost("adjust")]
    [HttpPost("adjustment")]
    [HttpPost("/api/stock-log/adjust")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<StockLogDto>>> AdjustStock([FromBody] CreateStockAdjustmentRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _stockLogRepository.RecordAdjustmentAsync(request, userId);
        return Ok(ApiResponse<StockLogDto>.Ok(result, "Stock adjusted successfully."));
    }
}
```

### `SalesController.cs`: Point of Sale Checkout
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;

    public SalesController(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<SaleDto>>> CreateSale([FromBody] CreateSaleRequest request)
    {
        var userId = GetCurrentUserId();
        var sale = await _saleRepository.CreateSaleAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = sale.SaleID }, ApiResponse<SaleDto>.Ok(sale));
    }
}
```

---

In the next chapter, **[05: Frontend Architecture & UI Walkthrough](file:///home/noir/Work/AL-Dawah_Pharma/documentation/05_frontend_javascript_and_ui_walkthrough.md)**, we examine how the frontend application renders this data in the browser.
