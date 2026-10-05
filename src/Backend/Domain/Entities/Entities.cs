using AlDawahPharma.Domain.Enums;

namespace AlDawahPharma.Domain.Entities;

public class User
{
    public long UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Supplier
{
    public long SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Company
{
    public long CompanyID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class Category
{
    public long CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class Customer
{
    public long CustomerID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class Medicine
{
    public long MedicineID { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public long CategoryID { get; set; }
    public long CompanyID { get; set; }
    public long SupplierID { get; set; }
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

    // Navigation / Display properties
    public string? CategoryName { get; set; }
    public string? CompanyName { get; set; }
    public string? SupplierName { get; set; }
}

public class Purchase
{
    public long PurchaseID { get; set; }
    public long SupplierID { get; set; }
    public long UserID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }

    // Display & Child items
    public string? SupplierName { get; set; }
    public string? UserName { get; set; }
    public List<PurchaseDetail> Details { get; set; } = new();
}

public class PurchaseDetail
{
    public long PurchaseDetailID { get; set; }
    public long PurchaseID { get; set; }
    public long MedicineID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    // Display properties
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
}

public class Sale
{
    public long SaleID { get; set; }
    public long CustomerID { get; set; }
    public long UserID { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }

    // Display & Child items
    public string? CustomerName { get; set; }
    public string? UserName { get; set; }
    public List<SaleDetail> Details { get; set; } = new();
}

public class SaleDetail
{
    public long SaleDetailID { get; set; }
    public long SaleID { get; set; }
    public long MedicineID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    // Display properties
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
}

public class StockLog
{
    public long LogID { get; set; }
    public long MedicineID { get; set; }
    public long UserID { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ActionDate { get; set; }
    public string? Remarks { get; set; }

    // Display properties
    public string? MedicineName { get; set; }
    public string? UserName { get; set; }
}

public class ExpiryAlert
{
    public long AlertID { get; set; }
    public long MedicineID { get; set; }
    public DateTime AlertDate { get; set; }
    public string AlertStatus { get; set; } = string.Empty;
    public string NotificationSent { get; set; } = "N";

    // Display properties
    public string? MedicineName { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? QuantityInStock { get; set; }
}
