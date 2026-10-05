-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 03_views.sql
-- Description: The 7 core analytical and operational database views
-- Source of Truth: Oracle Database
-- =============================================================================

-- 1. ExpiredMedicine_View: Medicines whose expiry date has passed (< SYSDATE)
CREATE OR REPLACE VIEW ExpiredMedicine_View AS
SELECT
    MedicineID,
    MedicineName,
    GenericName,
    BatchNumber,
    QuantityInStock,
    SellingPrice,
    ExpiryDate
FROM Medicine
WHERE ExpiryDate < SYSDATE;

-- 2. NearExpiryMedicine_View: Medicines approaching expiry within 30 days
CREATE OR REPLACE VIEW NearExpiryMedicine_View AS
SELECT
    MedicineID,
    MedicineName,
    GenericName,
    BatchNumber,
    QuantityInStock,
    SellingPrice,
    ExpiryDate
FROM Medicine
WHERE ExpiryDate >= SYSDATE
  AND ExpiryDate <= SYSDATE + 30;

-- 3. LowStock_View: Medicines where QuantityInStock is at or below ReorderLevel
CREATE OR REPLACE VIEW LowStock_View AS
SELECT
    MedicineID,
    MedicineName,
    GenericName,
    QuantityInStock,
    ReorderLevel,
    SellingPrice
FROM Medicine
WHERE QuantityInStock <= ReorderLevel;

-- 4. MonthlySales_View: Monthly aggregate sales order count and revenue
CREATE OR REPLACE VIEW MonthlySales_View AS
SELECT
    EXTRACT(YEAR FROM SaleDate) AS SaleYear,
    EXTRACT(MONTH FROM SaleDate) AS SaleMonth,
    COUNT(SaleID) AS TotalSales,
    SUM(TotalAmount) AS TotalSalesAmount
FROM Sales
GROUP BY
    EXTRACT(YEAR FROM SaleDate),
    EXTRACT(MONTH FROM SaleDate);

-- 5. InventoryValue_View: Inventory value per medicine (QuantityInStock * PurchasePrice)
CREATE OR REPLACE VIEW InventoryValue_View AS
SELECT
    MedicineID,
    MedicineName,
    QuantityInStock,
    PurchasePrice,
    (QuantityInStock * PurchasePrice) AS InventoryValue
FROM Medicine;

-- 6. CompanyWiseStock_View: Stock information grouped by pharmaceutical company
CREATE OR REPLACE VIEW CompanyWiseStock_View AS
SELECT
    c.CompanyID,
    c.CompanyName,
    COUNT(m.MedicineID) AS TotalMedicines,
    NVL(SUM(m.QuantityInStock), 0) AS TotalStock
FROM Company c
LEFT JOIN Medicine m
    ON c.CompanyID = m.CompanyID
GROUP BY
    c.CompanyID,
    c.CompanyName;

-- 7. SupplierWisePurchase_View: Purchase totals and volume grouped by supplier
CREATE OR REPLACE VIEW SupplierWisePurchase_View AS
SELECT
    s.SupplierID,
    s.SupplierName,
    COUNT(p.PurchaseID) AS TotalPurchases,
    NVL(SUM(p.TotalAmount), 0) AS TotalPurchaseAmount
FROM Supplier s
LEFT JOIN Purchase p
    ON s.SupplierID = p.SupplierID
GROUP BY
    s.SupplierID,
    s.SupplierName;
