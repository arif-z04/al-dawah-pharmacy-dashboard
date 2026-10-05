-- expired medicine
SELECT 
    MedicineID,
    MedicineName,
    BatchNumber,
    QuantityInStock,
    ExpiryDate
FROM Medicine
WHERE ExpiryDate < SYSDATE
ORDER BY ExpiryDate;

-- new expiry medicine
SELECT 
    MedicineID,
    MedicineName,
    BatchNumber,
    QuantityInStock,
    ExpiryDate
FROM Medicine
WHERE ExpiryDate >= SYSDATE
  AND ExpiryDate <= SYSDATE + 30
ORDER BY ExpiryDate;

-- Low Stock Medicine
SELECT 
    MedicineID,
    MedicineName,
    QuantityInStock,
    ReorderLevel
FROM Medicine
WHERE QuantityInStock <= ReorderLevel
ORDER BY QuantityInStock;

--Monthly Sales
SELECT 
    TO_CHAR(SaleDate, 'YYYY-MM') AS SalesMonth,
    COUNT(SaleID) AS TotalSales,
    SUM(TotalAmount) AS TotalRevenue
FROM Sales
GROUP BY TO_CHAR(SaleDate, 'YYYY-MM')
ORDER BY SalesMonth;
-- Highest selling medicine
SELECT 
    m.MedicineID,
    m.MedicineName,
    SUM(sd.Quantity) AS TotalQuantitySold
FROM SalesDetails sd
JOIN Medicine m
    ON sd.MedicineID = m.MedicineID
GROUP BY 
    m.MedicineID,
    m.MedicineName
ORDER BY TotalQuantitySold DESC;
--Top 1 highest selling medicine
SELECT *
FROM (
    SELECT 
        m.MedicineID,
        m.MedicineName,
        SUM(sd.Quantity) AS TotalQuantitySold
    FROM SalesDetails sd
    JOIN Medicine m
        ON sd.MedicineID = m.MedicineID
    GROUP BY 
        m.MedicineID,
        m.MedicineName
    ORDER BY TotalQuantitySold DESC
)
WHERE ROWNUM = 1;

--Company Wise Stock
SELECT 
    c.CompanyID,
    c.CompanyName,
    SUM(m.QuantityInStock) AS TotalStock
FROM Medicine m
JOIN Company c
    ON m.CompanyID = c.CompanyID
GROUP BY 
    c.CompanyID,
    c.CompanyName
ORDER BY TotalStock DESC;

-- Supplier Wise Purchase

SELECT 
    s.SupplierID,
    s.SupplierName,
    COUNT(DISTINCT p.PurchaseID) AS TotalPurchases,
    SUM(pd.Quantity) AS TotalQuantityPurchased,
    SUM(pd.SubTotal) AS TotalPurchaseAmount
FROM Supplier s
JOIN Purchase p
    ON s.SupplierID = p.SupplierID
JOIN PurchaseDetails pd
    ON p.PurchaseID = pd.PurchaseID
GROUP BY 
    s.SupplierID,
    s.SupplierName
ORDER BY TotalPurchaseAmount DESC;

-- invertory value
SELECT 
    SUM(QuantityInStock * PurchasePrice) AS TotalInventoryValue
FROM Medicine;

SELECT 
    MedicineID,
    MedicineName,
    QuantityInStock,
    PurchasePrice,
    (QuantityInStock * PurchasePrice) AS InventoryValue
FROM Medicine
ORDER BY InventoryValue DESC;

--Company-wise Inventory Value
SELECT 
    c.CompanyName,
    SUM(m.QuantityInStock) AS TotalStock,
    SUM(m.QuantityInStock * m.PurchasePrice) AS InventoryValue
FROM Medicine m
JOIN Company c
    ON m.CompanyID = c.CompanyID
GROUP BY c.CompanyName
ORDER BY InventoryValue DESC;

--Bonus: Customer-wise Sales
SELECT 
    c.CustomerID,
    c.CustomerName,
    COUNT(s.SaleID) AS TotalOrders,
    SUM(s.TotalAmount) AS TotalSpent
FROM Customer c
JOIN Sales s
    ON c.CustomerID = s.CustomerID
GROUP BY 
    c.CustomerID,
    c.CustomerName
ORDER BY TotalSpent DESC;

--Bonus: Pharmacist/User-wise Sales

SELECT 
    u.UserID,
    u.UserName,
    COUNT(s.SaleID) AS TotalSales,
    SUM(s.TotalAmount) AS TotalRevenue
FROM Users u
JOIN Sales s
    ON u.UserID = s.UserID
GROUP BY 
    u.UserID,
    u.UserName
ORDER BY TotalRevenue DESC;


