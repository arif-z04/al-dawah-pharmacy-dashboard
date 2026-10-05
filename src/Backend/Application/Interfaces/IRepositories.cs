using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Domain.Entities;

namespace AlDawahPharma.Application.Interfaces;

public interface IOracleConnectionFactory
{
    IDbConnection CreateConnection();
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public interface IJwtTokenService
{
    string GenerateToken(User user);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(CreateUserRequest request);
    Task<bool> UpdateAsync(long userId, UpdateUserRequest request);
    Task<bool> ChangePasswordAsync(long userId, string hashedPassword);
    Task<bool> DeleteAsync(long userId);
}

public interface ICategoryRepository
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(long categoryId);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<bool> UpdateAsync(long categoryId, string categoryName);
    Task<bool> DeleteAsync(long categoryId);
}

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<CompanyDto?> GetByIdAsync(long companyId);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request);
    Task<bool> UpdateAsync(long companyId, UpdateCompanyRequest request);
    Task<bool> DeleteAsync(long companyId);
}

public interface ISupplierRepository
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(long supplierId);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request);
    Task<bool> UpdateAsync(long supplierId, UpdateSupplierRequest request);
    Task<bool> DeleteAsync(long supplierId);
}

public interface ICustomerRepository
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(long customerId);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<bool> UpdateAsync(long customerId, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(long customerId);
}

public interface IMedicineRepository
{
    Task<IEnumerable<MedicineDto>> GetAllAsync(string? searchTerm = null, long? categoryId = null, long? companyId = null);
    Task<MedicineDto?> GetByIdAsync(long medicineId);
    Task<MedicineDto?> GetByBatchNumberAsync(string batchNumber);
    Task<MedicineDto> CreateAsync(CreateMedicineRequest request);
    Task<bool> UpdateAsync(long medicineId, UpdateMedicineRequest request);
    Task<bool> DeleteAsync(long medicineId);
    Task<int> GetAvailableStockAsync(long medicineId);
    Task<decimal> GetInventoryValueAsync(long medicineId);
}

public interface IPurchaseRepository
{
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto?> GetByIdAsync(long purchaseId);
    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, long userId);
}

public interface ISaleRepository
{
    Task<IEnumerable<SaleDto>> GetAllAsync();
    Task<SaleDto?> GetByIdAsync(long saleId);
    Task<SaleDto> CreateAsync(CreateSaleRequest request, long userId);
}

public interface IStockLogRepository
{
    Task<IEnumerable<StockLogDto>> GetAllAsync(long? medicineId = null, string? actionType = null);
    Task<StockLogDto> RecordAdjustmentAsync(CreateStockAdjustmentRequest request, long userId);
}

public interface IExpiryAlertRepository
{
    Task<IEnumerable<ExpiryAlertDto>> GetAllAsync(string? status = null);
    Task<bool> UpdateNotificationStatusAsync(long alertId, string notificationSent);
    Task<int> RefreshAlertsAsync();
}

public interface IReportRepository
{
    Task<IEnumerable<ExpiredMedicineReportDto>> GetExpiredMedicinesAsync();
    Task<IEnumerable<NearExpiryReportDto>> GetNearExpiryMedicinesAsync();
    Task<IEnumerable<LowStockReportDto>> GetLowStockMedicinesAsync();
    Task<IEnumerable<MonthlySalesReportDto>> GetMonthlySalesAsync();
    Task<IEnumerable<TopSellingMedicineDto>> GetTopSellingMedicinesAsync(int top = 10);
    Task<IEnumerable<CompanyStockReportDto>> GetCompanyStockAsync();
    Task<IEnumerable<SupplierPurchaseReportDto>> GetSupplierPurchasesAsync();
    Task<IEnumerable<InventoryValuationReportDto>> GetInventoryValuationAsync();
}

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}
