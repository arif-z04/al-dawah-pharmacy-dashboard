# Al-Dawah Pharma - Database Architecture & Schema Specification

## 1. Overview
The **Oracle Database** is the definitive source of truth for **Al-Dawah Pharma**. It contains exactly **12 core relational tables**, **7 analytical views**, **4 stored procedures**, **2 deterministic functions**, and **6 automated triggers**.

---

## 2. Core 12 Tables

| # | Table Name | Description | Key Columns |
|---|------------|-------------|-------------|
| 1 | `USERS` | System user accounts, credentials, and roles | `UserID` (PK), `Username` (UQ), `Password` (BCrypt), `Role` |
| 2 | `SUPPLIER` | Pharmaceutical distributors and vendor contacts | `SupplierID` (PK), `SupplierName`, `ContactPerson`, `Phone` |
| 3 | `COMPANY` | Pharmaceutical manufacturing corporations | `CompanyID` (PK), `CompanyName` (UQ), `Phone`, `Email` |
| 4 | `CATEGORY` | Dosage forms (Tablet, Capsule, Syrup, etc.) | `CategoryID` (PK), `CategoryName` (UQ) |
| 5 | `CUSTOMER` | Retail customer profiles | `CustomerID` (PK), `CustomerName`, `Phone`, `Address` |
| 6 | `MEDICINE` | Drug catalog, pricing, inventory stock, batch | `MedicineID` (PK), `BatchNumber` (UQ), `PurchasePrice`, `SellingPrice`, `QuantityInStock`, `ReorderLevel`, `ExpiryDate` |
| 7 | `PURCHASE` | Wholesale purchase invoice headers | `PurchaseID` (PK), `SupplierID` (FK), `UserID` (FK), `TotalAmount` |
| 8 | `PURCHASEDETAILS` | Line items belonging to purchase orders | `PurchaseDetailID` (PK), `PurchaseID` (FK), `MedicineID` (FK), `Quantity`, `UnitPrice`, `SubTotal` |
| 9 | `SALES` | Retail sales invoice headers | `SaleID` (PK), `CustomerID` (FK), `UserID` (FK), `TotalAmount` |
| 10 | `SALESDETAILS` | Line items belonging to sales invoices | `SaleDetailID` (PK), `SaleID` (FK), `MedicineID` (FK), `Quantity`, `UnitPrice`, `SubTotal` |
| 11 | `STOCKLOG` | Audit log of all inventory movements | `LogID` (PK), `MedicineID` (FK), `UserID` (FK), `ActionType` ('Purchase', 'Sale', 'Adjustment'), `Quantity`, `ActionDate` |
| 12 | `EXPIRYALERT` | Notifications and status for expiring medicines | `AlertID` (PK), `MedicineID` (FK), `AlertDate`, `AlertStatus` ('Near Expiry', 'Expired'), `NotificationSent` ('Y', 'N') |

---

## 3. The 7 Core Views

1. **`ExpiredMedicine_View`**: Returns medicines whose `ExpiryDate < SYSDATE`.
2. **`NearExpiryMedicine_View`**: Returns medicines approaching expiration (`ExpiryDate >= SYSDATE AND ExpiryDate <= SYSDATE + 30`).
3. **`LowStock_View`**: Returns medicines where `QuantityInStock <= ReorderLevel`.
4. **`MonthlySales_View`**: Aggregates sales by Year and Month (`COUNT(SaleID)`, `SUM(TotalAmount)`).
5. **`InventoryValue_View`**: Computes inventory valuation per SKU (`QuantityInStock * PurchasePrice`).
6. **`CompanyWiseStock_View`**: Groups medicines and total available stock units by pharmaceutical manufacturing company.
7. **`SupplierWisePurchase_View`**: Groups purchases, order counts, and total expenditure by wholesale supplier.

---

## 4. Stored Procedures (4)

1. **`ADD_CATEGORY(p_categoryid, p_categoryname)`**:
   Validates category ID and name uniqueness, trims whitespace, and creates the category.
2. **`ADD_MEDICINE(...)`**:
   Validates foreign keys (`CategoryID`, `CompanyID`, `SupplierID`), validates non-negative pricing, enforces `SellingPrice >= PurchasePrice`, verifies `ExpiryDate > ManufacturingDate`, and records the new medicine.
3. **`RECORD_PURCHASE(p_purchaseid, p_supplierid, p_userid, p_medicineid, p_quantity, p_unitprice)`**:
   Records purchase header and line item. Relies on triggers for subtotal calculation, stock increment, and invoice totals to eliminate duplicated code.
4. **`RECORD_SALE(p_saleid, p_customerid, p_userid, p_medicineid, p_quantity, p_unitprice)`**:
   Records sales header and line item. Stock availability is verified by `TRG_SALE_STOCK`; stock is decremented and audit log recorded automatically.

---

## 5. Functions (2)

1. **`GET_AVAILABLE_STOCK(p_medicineid NUMBER) RETURN NUMBER`**:
   Deterministic read-only function returning current available stock for a given medicine.
2. **`GET_INVENTORY_VALUE(p_medicineid NUMBER) RETURN NUMBER`**:
   Deterministic read-only function returning `QuantityInStock * PurchasePrice` for a given medicine.

---

## 6. Triggers (6)

1. **`TRG_PURCHASE_DETAIL_SUBTOTAL`**: `BEFORE INSERT OR UPDATE ON PurchaseDetails` — Sets `:NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice`.
2. **`TRG_PURCHASE_STOCK`**: `AFTER INSERT ON PurchaseDetails` — Increments `Medicine.QuantityInStock` and inserts a `StockLog` movement record (`ActionType = 'Purchase'`).
3. **`TRG_PURCHASE_TOTAL`**: `FOR INSERT/UPDATE/DELETE ON PurchaseDetails COMPOUND TRIGGER` — Updates `Purchase.TotalAmount` safely without mutating table errors (`ORA-04091`).
4. **`TRG_SALES_DETAIL_SUBTOTAL`**: `BEFORE INSERT OR UPDATE ON SalesDetails` — Sets `:NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice`.
5. **`TRG_SALE_STOCK`**: `AFTER INSERT ON SalesDetails` — Validates available stock. If `QuantityInStock < :NEW.Quantity`, raises `ORA-20001: Insufficient stock`! Otherwise decrements stock and inserts a `StockLog` record (`ActionType = 'Sale'`).
6. **`TRG_SALES_TOTAL`**: `FOR INSERT/UPDATE/DELETE ON SalesDetails COMPOUND TRIGGER` — Updates `Sales.TotalAmount` safely.

---

## 7. Security & Role-Based Access Control

- **`C##PHARMACY_ADMIN`**: Full CRUD permissions on all 12 tables and 7 views; execute permissions on all procedures and functions.
- **`C##PHARMACY_STAFF`**: Read permissions on catalog/audit tables; insert/update on operational tables (`Customer`, `Purchase`, `Sales`); execute permissions on operational routines; no destructive drop/delete privileges.
- **`C##PHARMACY_APP`**: Dedicated application database user. The web API connects exclusively using this account. Public synonyms are registered for all schema objects to eliminate hardcoded schema prefixes.
