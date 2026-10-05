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
    CREATE OR REPLACE VIEW InventoryValue_View AS
SELECT
    MedicineID,
    MedicineName,
    QuantityInStock,
    PurchasePrice,
    (QuantityInStock * PurchasePrice) AS InventoryValue
FROM Medicine;

CREATE OR REPLACE VIEW CompanyWiseStock_View AS
SELECT
    c.CompanyID,
    c.CompanyName,
    COUNT(m.MedicineID) AS TotalMedicines,
    SUM(m.QuantityInStock) AS TotalStock
FROM Company c
LEFT JOIN Medicine m
    ON c.CompanyID = m.CompanyID
GROUP BY
    c.CompanyID,
    c.CompanyName;
    
   CREATE OR REPLACE VIEW SupplierWisePurchase_View AS
SELECT
    s.SupplierID,
    s.SupplierName,
    COUNT(p.PurchaseID) AS TotalPurchases,
    SUM(p.TotalAmount) AS TotalPurchaseAmount
FROM Supplier s
LEFT JOIN Purchase p
    ON s.SupplierID = p.SupplierID
GROUP BY
    s.SupplierID,
    s.SupplierName; 