-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 09_reports_queries.sql
-- Description: The 8 Core Business Reporting Queries & Analytics
-- Source of Truth: Oracle Database
-- =============================================================================

-- -----------------------------------------------------------------------------
-- REPORT 1: Expired Medicine Report
-- Identifies medicines whose expiry date has passed (< SYSDATE)
-- -----------------------------------------------------------------------------
SELECT 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    c.CategoryName,
    cp.CompanyName,
    m.BatchNumber,
    m.QuantityInStock,
    m.PurchasePrice,
    m.SellingPrice,
    m.ExpiryDate,
    ROUND(SYSDATE - m.ExpiryDate) AS DaysExpired
FROM Medicine m
JOIN Category c ON m.CategoryID = c.CategoryID
JOIN Company cp ON m.CompanyID = cp.CompanyID
WHERE m.ExpiryDate < SYSDATE
ORDER BY m.ExpiryDate ASC;

-- -----------------------------------------------------------------------------
-- REPORT 2: Near Expiry Medicine Report
-- Identifies medicines expiring within the next 30 days
-- -----------------------------------------------------------------------------
SELECT 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    c.CategoryName,
    cp.CompanyName,
    m.BatchNumber,
    m.QuantityInStock,
    m.SellingPrice,
    m.ExpiryDate,
    ROUND(m.ExpiryDate - SYSDATE) AS DaysRemaining
FROM Medicine m
JOIN Category c ON m.CategoryID = c.CategoryID
JOIN Company cp ON m.CompanyID = cp.CompanyID
WHERE m.ExpiryDate >= SYSDATE
  AND m.ExpiryDate <= SYSDATE + 30
ORDER BY m.ExpiryDate ASC;

-- -----------------------------------------------------------------------------
-- REPORT 3: Low Stock Medicine Report
-- Identifies medicines where QuantityInStock is at or below the ReorderLevel
-- -----------------------------------------------------------------------------
SELECT 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    c.CategoryName,
    cp.CompanyName,
    s.SupplierName,
    m.QuantityInStock,
    m.ReorderLevel,
    (m.ReorderLevel - m.QuantityInStock) AS StockDeficit,
    m.SellingPrice
FROM Medicine m
JOIN Category c ON m.CategoryID = c.CategoryID
JOIN Company cp ON m.CompanyID = cp.CompanyID
JOIN Supplier s ON m.SupplierID = s.SupplierID
WHERE m.QuantityInStock <= m.ReorderLevel
ORDER BY StockDeficit DESC, m.QuantityInStock ASC;

-- -----------------------------------------------------------------------------
-- REPORT 4: Monthly Sales Summary Report
-- Aggregates sales orders, unit volumes, and gross revenue by month
-- -----------------------------------------------------------------------------
SELECT 
    TO_CHAR(s.SaleDate, 'YYYY-MM') AS SalesMonth,
    COUNT(DISTINCT s.SaleID) AS TotalInvoices,
    NVL(SUM(sd.Quantity), 0) AS TotalUnitsSold,
    NVL(SUM(s.TotalAmount), 0) AS TotalRevenue
FROM Sales s
LEFT JOIN SalesDetails sd ON s.SaleID = sd.SaleID
GROUP BY TO_CHAR(s.SaleDate, 'YYYY-MM')
ORDER BY SalesMonth DESC;

-- -----------------------------------------------------------------------------
-- REPORT 5: Highest Selling Medicines Report
-- Identifies top-selling medicines ranked by total units sold and revenue generated
-- -----------------------------------------------------------------------------
SELECT 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    c.CategoryName,
    cp.CompanyName,
    SUM(sd.Quantity) AS TotalQuantitySold,
    SUM(sd.SubTotal) AS TotalRevenueGenerated
FROM SalesDetails sd
JOIN Medicine m ON sd.MedicineID = m.MedicineID
JOIN Category c ON m.CategoryID = c.CategoryID
JOIN Company cp ON m.CompanyID = cp.CompanyID
GROUP BY 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    c.CategoryName,
    cp.CompanyName
ORDER BY TotalQuantitySold DESC;

-- -----------------------------------------------------------------------------
-- REPORT 6: Company-Wise Stock Report
-- Aggregates current stock count, total inventory units, and inventory valuation per company
-- -----------------------------------------------------------------------------
SELECT 
    cp.CompanyID,
    cp.CompanyName,
    COUNT(m.MedicineID) AS TotalMedicineItems,
    NVL(SUM(m.QuantityInStock), 0) AS TotalStockUnits,
    NVL(SUM(m.QuantityInStock * m.PurchasePrice), 0) AS TotalInventoryValue
FROM Company cp
LEFT JOIN Medicine m ON cp.CompanyID = m.CompanyID
GROUP BY 
    cp.CompanyID,
    cp.CompanyName
ORDER BY TotalInventoryValue DESC;

-- -----------------------------------------------------------------------------
-- REPORT 7: Supplier-Wise Purchase Report
-- Aggregates total purchase orders, quantity received, and expenditure per supplier
-- -----------------------------------------------------------------------------
SELECT 
    s.SupplierID,
    s.SupplierName,
    COUNT(DISTINCT p.PurchaseID) AS TotalPurchaseOrders,
    NVL(SUM(pd.Quantity), 0) AS TotalQuantityPurchased,
    NVL(SUM(pd.SubTotal), 0) AS TotalPurchaseExpenditure
FROM Supplier s
LEFT JOIN Purchase p ON s.SupplierID = p.SupplierID
LEFT JOIN PurchaseDetails pd ON p.PurchaseID = pd.PurchaseID
GROUP BY 
    s.SupplierID,
    s.SupplierName
ORDER BY TotalPurchaseExpenditure DESC;

-- -----------------------------------------------------------------------------
-- REPORT 8: Inventory Valuation Report
-- Detailed stock valuation breakdown per medicine item
-- -----------------------------------------------------------------------------
SELECT 
    m.MedicineID,
    m.MedicineName,
    m.GenericName,
    m.BatchNumber,
    m.QuantityInStock,
    m.PurchasePrice,
    (m.QuantityInStock * m.PurchasePrice) AS InventoryValuationAtCost,
    m.SellingPrice,
    (m.QuantityInStock * m.SellingPrice) AS ProjectedRevenueAtRetail,
    ((m.QuantityInStock * m.SellingPrice) - (m.QuantityInStock * m.PurchasePrice)) AS ProjectedGrossMargin
FROM Medicine m
ORDER BY InventoryValuationAtCost DESC;

-- Overall Total Inventory Valuation Summary
SELECT 
    COUNT(MedicineID) AS TotalDistinctMedicines,
    SUM(QuantityInStock) AS TotalInventoryUnits,
    SUM(QuantityInStock * PurchasePrice) AS GrandTotalValuationAtCost,
    SUM(QuantityInStock * SellingPrice) AS GrandTotalValuationAtRetail
FROM Medicine;

-- -----------------------------------------------------------------------------
-- BONUS ANALYTICS: Customer Sales Ranking
-- -----------------------------------------------------------------------------
SELECT 
    c.CustomerID,
    c.CustomerName,
    c.Phone,
    COUNT(s.SaleID) AS TotalInvoices,
    NVL(SUM(s.TotalAmount), 0) AS TotalExpenditure
FROM Customer c
JOIN Sales s ON c.CustomerID = s.CustomerID
GROUP BY 
    c.CustomerID,
    c.CustomerName,
    c.Phone
ORDER BY TotalExpenditure DESC;

-- -----------------------------------------------------------------------------
-- BONUS ANALYTICS: Pharmacist / Staff Sales Performance
-- -----------------------------------------------------------------------------
SELECT 
    u.UserID,
    u.FullName,
    u.Username,
    u.Role,
    COUNT(s.SaleID) AS TotalInvoicesProcessed,
    NVL(SUM(s.TotalAmount), 0) AS TotalRevenueHandled
FROM Users u
JOIN Sales s ON u.UserID = s.UserID
GROUP BY 
    u.UserID,
    u.FullName,
    u.Username,
    u.Role
ORDER BY TotalRevenueHandled DESC;
