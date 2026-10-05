using AlDawahPharma.Application.Interfaces;
using AlDawahPharma.Infrastructure.Auth;
using AlDawahPharma.Infrastructure.Data;
using AlDawahPharma.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AlDawahPharma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IOracleConnectionFactory, OracleConnectionFactory>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IStockLogRepository, StockLogRepository>();
        services.AddScoped<IExpiryAlertRepository, ExpiryAlertRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }
}
