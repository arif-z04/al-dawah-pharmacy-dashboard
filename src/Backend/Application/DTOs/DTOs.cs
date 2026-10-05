namespace AlDawahPharma.Application.DTOs;

// -----------------------------------------------------------------------------
// Authentication & User DTOs
// -----------------------------------------------------------------------------
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public long UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class UserDto
{
    public long UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

// -----------------------------------------------------------------------------
// Category DTOs
// -----------------------------------------------------------------------------
public class CategoryDto
{
    public long CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
}

public class CreateCategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
}

// -----------------------------------------------------------------------------
// Company DTOs
// -----------------------------------------------------------------------------
public class CompanyDto
{
    public long CompanyID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int MedicineCount { get; set; }
}

public class CreateCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateCompanyRequest : CreateCompanyRequest { }

// -----------------------------------------------------------------------------
// Supplier DTOs
// -----------------------------------------------------------------------------
public class SupplierDto
{
    public long SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class UpdateSupplierRequest : CreateSupplierRequest { }

// -----------------------------------------------------------------------------
// Customer DTOs
// -----------------------------------------------------------------------------
public class CustomerDto
{
    public long CustomerID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class CreateCustomerRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UpdateCustomerRequest : CreateCustomerRequest { }

// -----------------------------------------------------------------------------
// Medicine DTOs
// -----------------------------------------------------------------------------
public class MedicineDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public long CategoryID { get; set; }
    public string? CategoryName { get; set; }
    public long CompanyID { get; set; }
    public string? CompanyName { get; set; }
    public long SupplierID { get; set; }
    public string? SupplierName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal InventoryValue => QuantityInStock * PurchasePrice;
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
    public bool IsExpired => ExpiryDate < DateTime.UtcNow.Date;
    public bool IsNearExpiry => ExpiryDate >= DateTime.UtcNow.Date && ExpiryDate <= DateTime.UtcNow.Date.AddDays(30);
}

public class CreateMedicineRequest
{
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public long CategoryID { get; set; }
    public long CompanyID { get; set; }
    public long SupplierID { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
}

public class UpdateMedicineRequest
{
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public long CategoryID { get; set; }
    public long CompanyID { get; set; }
    public long SupplierID { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
}

// -----------------------------------------------------------------------------
// Purchase DTOs
// -----------------------------------------------------------------------------
public class PurchaseDto
{
    public long PurchaseID { get; set; }
    public long SupplierID { get; set; }
    public string? SupplierName { get; set; }
    public long UserID { get; set; }
    public string? UserName { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseDetailDto> Details { get; set; } = new();
}

public class PurchaseDetailDto
{
    public long PurchaseDetailID { get; set; }
    public long PurchaseID { get; set; }
    public long MedicineID { get; set; }
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreatePurchaseRequest
{
    public long SupplierID { get; set; }
    public List<PurchaseItemRequest> Items { get; set; } = new();
}

public class PurchaseItemRequest
{
    public long MedicineID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// -----------------------------------------------------------------------------
// Sale DTOs
// -----------------------------------------------------------------------------
public class SaleDto
{
    public long SaleID { get; set; }
    public long CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public long UserID { get; set; }
    public string? UserName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public long SaleDetailID { get; set; }
    public long SaleID { get; set; }
    public long MedicineID { get; set; }
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class CreateSaleRequest
{
    public long CustomerID { get; set; }
    public List<SaleItemRequest> Items { get; set; } = new();
}

public class SaleItemRequest
{
    public long MedicineID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// -----------------------------------------------------------------------------
// Stock Log DTOs
// -----------------------------------------------------------------------------
public class StockLogDto
{
    public long LogID { get; set; }
    public long MedicineID { get; set; }
    public string? MedicineName { get; set; }
    public long UserID { get; set; }
    public string? UserName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ActionDate { get; set; }
    public string? Remarks { get; set; }
}

public class CreateStockAdjustmentRequest
{
    public long MedicineID { get; set; }
    public int Quantity { get; set; } // positive or negative
    public string? Reason { get; set; } // Damaged, Inventory Count, Return, Expired
    public string? Remarks { get; set; }
}

// -----------------------------------------------------------------------------
// Expiry Alert DTOs
// -----------------------------------------------------------------------------
public class ExpiryAlertDto
{
    public long AlertID { get; set; }
    public long MedicineID { get; set; }
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? QuantityInStock { get; set; }
    public DateTime AlertDate { get; set; }
    public string AlertStatus { get; set; } = string.Empty;
    public string NotificationSent { get; set; } = "N";
}

public class UpdateAlertStatusRequest
{
    public string NotificationSent { get; set; } = "Y";
}

// -----------------------------------------------------------------------------
// Reporting & Analytics DTOs
// -----------------------------------------------------------------------------
public class ExpiredMedicineReportDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? CompanyName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
    public decimal LossValue => QuantityInStock * PurchasePrice;
}

public class NearExpiryReportDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? CompanyName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal SellingPrice { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
}

public class LowStockReportDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? CompanyName { get; set; }
    public string? SupplierName { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public int StockDeficit { get; set; }
    public decimal SellingPrice { get; set; }
}

public class MonthlySalesReportDto
{
    public string SalesMonth { get; set; } = string.Empty; // YYYY-MM
    public int TotalInvoices { get; set; }
    public int TotalUnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopSellingMedicineDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? CategoryName { get; set; }
    public string? CompanyName { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
}

public class CompanyStockReportDto
{
    public long CompanyID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int TotalMedicineItems { get; set; }
    public int TotalStockUnits { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class SupplierPurchaseReportDto
{
    public long SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int TotalPurchaseOrders { get; set; }
    public int TotalQuantityPurchased { get; set; }
    public decimal TotalPurchaseExpenditure { get; set; }
}

public class InventoryValuationReportDto
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal InventoryValuationAtCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal ProjectedRevenueAtRetail { get; set; }
    public decimal ProjectedGrossMargin { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalMedicines { get; set; }
    public int TotalCategories { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalCustomers { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiredCount { get; set; }
    public int NearExpiryCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalSalesRevenue { get; set; }
    public decimal TotalPurchasesExpenditure { get; set; }
    public int TotalSalesCount { get; set; }
    public int TotalPurchasesCount { get; set; }
    public List<MonthlySalesReportDto> RecentSalesChart { get; set; } = new();
    public List<TopSellingMedicineDto> TopSellingMedicines { get; set; } = new();
}
