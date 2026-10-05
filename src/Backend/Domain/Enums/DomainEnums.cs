namespace AlDawahPharma.Domain.Enums;

public enum UserRole
{
    Admin,
    Pharmacist,
    Staff
}

public enum StockActionType
{
    Purchase,
    Sale,
    Adjustment
}

public enum AlertStatus
{
    NearExpiry,
    Expired
}
