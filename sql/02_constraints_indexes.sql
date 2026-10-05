-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 02_constraints_indexes.sql
-- Description: Foreign Key Constraints, CHECK Constraints, and Performance Indexes
-- Source of Truth: Oracle Database
-- =============================================================================

-- =============================================================================
-- 1. FOREIGN KEY CONSTRAINTS
-- =============================================================================

-- Medicine Foreign Keys
ALTER TABLE Medicine
    ADD CONSTRAINT fk_med_category
    FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID);

ALTER TABLE Medicine
    ADD CONSTRAINT fk_med_company
    FOREIGN KEY (CompanyID) REFERENCES Company(CompanyID);

ALTER TABLE Medicine
    ADD CONSTRAINT fk_med_supplier
    FOREIGN KEY (SupplierID) REFERENCES Supplier(SupplierID);

-- Purchase Foreign Keys
ALTER TABLE Purchase
    ADD CONSTRAINT fk_purchase_supplier
    FOREIGN KEY (SupplierID) REFERENCES Supplier(SupplierID);

ALTER TABLE Purchase
    ADD CONSTRAINT fk_purchase_user
    FOREIGN KEY (UserID) REFERENCES Users(UserID);

-- PurchaseDetails Foreign Keys
ALTER TABLE PurchaseDetails
    ADD CONSTRAINT fk_pd_purchase
    FOREIGN KEY (PurchaseID) REFERENCES Purchase(PurchaseID)
    ON DELETE CASCADE;

ALTER TABLE PurchaseDetails
    ADD CONSTRAINT fk_pd_medicine
    FOREIGN KEY (MedicineID) REFERENCES Medicine(MedicineID);

-- Sales Foreign Keys
ALTER TABLE Sales
    ADD CONSTRAINT fk_sales_customer
    FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID);

ALTER TABLE Sales
    ADD CONSTRAINT fk_sales_user
    FOREIGN KEY (UserID) REFERENCES Users(UserID);

-- SalesDetails Foreign Keys
ALTER TABLE SalesDetails
    ADD CONSTRAINT fk_sd_sale
    FOREIGN KEY (SaleID) REFERENCES Sales(SaleID)
    ON DELETE CASCADE;

ALTER TABLE SalesDetails
    ADD CONSTRAINT fk_sd_medicine
    FOREIGN KEY (MedicineID) REFERENCES Medicine(MedicineID);

-- StockLog Foreign Keys
ALTER TABLE StockLog
    ADD CONSTRAINT fk_stocklog_medicine
    FOREIGN KEY (MedicineID) REFERENCES Medicine(MedicineID);

ALTER TABLE StockLog
    ADD CONSTRAINT fk_stocklog_user
    FOREIGN KEY (UserID) REFERENCES Users(UserID);

-- ExpiryAlert Foreign Keys
ALTER TABLE ExpiryAlert
    ADD CONSTRAINT fk_alert_medicine
    FOREIGN KEY (MedicineID) REFERENCES Medicine(MedicineID);


-- =============================================================================
-- 2. CHECK CONSTRAINTS
-- =============================================================================

-- Users role validation (Admin, Pharmacist, Staff)
ALTER TABLE Users
    ADD CONSTRAINT chk_user_role
    CHECK (Role IN ('Admin', 'Pharmacist', 'Staff'));

-- Medicine business constraints
ALTER TABLE Medicine
    ADD CONSTRAINT chk_med_purchase_price
    CHECK (PurchasePrice >= 0);

ALTER TABLE Medicine
    ADD CONSTRAINT chk_med_selling_price
    CHECK (SellingPrice >= 0);

ALTER TABLE Medicine
    ADD CONSTRAINT chk_med_quantity_stock
    CHECK (QuantityInStock >= 0);

ALTER TABLE Medicine
    ADD CONSTRAINT chk_med_reorder_level
    CHECK (ReorderLevel >= 0);

ALTER TABLE Medicine
    ADD CONSTRAINT chk_med_expiry_after_mfg
    CHECK (ExpiryDate > ManufacturingDate);

-- Purchase validation
ALTER TABLE Purchase
    ADD CONSTRAINT chk_purchase_total
    CHECK (TotalAmount >= 0);

-- PurchaseDetails validation
ALTER TABLE PurchaseDetails
    ADD CONSTRAINT chk_pd_quantity
    CHECK (Quantity > 0);

ALTER TABLE PurchaseDetails
    ADD CONSTRAINT chk_pd_unit_price
    CHECK (UnitPrice >= 0);

ALTER TABLE PurchaseDetails
    ADD CONSTRAINT chk_pd_subtotal
    CHECK (SubTotal >= 0);

-- Sales validation
ALTER TABLE Sales
    ADD CONSTRAINT chk_sales_total
    CHECK (TotalAmount >= 0);

-- SalesDetails validation
ALTER TABLE SalesDetails
    ADD CONSTRAINT chk_sd_quantity
    CHECK (Quantity > 0);

ALTER TABLE SalesDetails
    ADD CONSTRAINT chk_sd_unit_price
    CHECK (UnitPrice >= 0);

ALTER TABLE SalesDetails
    ADD CONSTRAINT chk_sd_subtotal
    CHECK (SubTotal >= 0);

-- StockLog validation
ALTER TABLE StockLog
    ADD CONSTRAINT chk_stocklog_action
    CHECK (ActionType IN ('Purchase', 'Sale', 'Adjustment'));

ALTER TABLE StockLog
    ADD CONSTRAINT chk_stocklog_quantity
    CHECK (Quantity > 0);

-- ExpiryAlert validation
ALTER TABLE ExpiryAlert
    ADD CONSTRAINT chk_alert_status
    CHECK (AlertStatus IN ('Near Expiry', 'Expired'));

ALTER TABLE ExpiryAlert
    ADD CONSTRAINT chk_alert_notification
    CHECK (NotificationSent IN ('Y', 'N'));


-- =============================================================================
-- 3. PERFORMANCE INDEXES
-- Purpose: Optimize relational JOINs, frequent searches, and analytical reports.
-- =============================================================================

-- Foreign Key Indexes (Oracle does not index FKs automatically; these prevent table locks during cascades and speed up JOINs)
CREATE INDEX idx_med_category ON Medicine(CategoryID);
CREATE INDEX idx_med_company ON Medicine(CompanyID);
CREATE INDEX idx_med_supplier ON Medicine(SupplierID);

CREATE INDEX idx_purchase_supplier ON Purchase(SupplierID);
CREATE INDEX idx_purchase_user ON Purchase(UserID);

CREATE INDEX idx_pd_purchase ON PurchaseDetails(PurchaseID);
CREATE INDEX idx_pd_medicine ON PurchaseDetails(MedicineID);

CREATE INDEX idx_sales_customer ON Sales(CustomerID);
CREATE INDEX idx_sales_user ON Sales(UserID);

CREATE INDEX idx_sd_sale ON SalesDetails(SaleID);
CREATE INDEX idx_sd_medicine ON SalesDetails(MedicineID);

CREATE INDEX idx_stocklog_medicine ON StockLog(MedicineID);
CREATE INDEX idx_stocklog_user ON StockLog(UserID);

CREATE INDEX idx_alert_medicine ON ExpiryAlert(MedicineID);

-- Search and Reporting Indexes:
-- idx_med_name: Accelerates autocomplete, catalog search, and name lookups
CREATE INDEX idx_med_name ON Medicine(UPPER(MedicineName));

-- idx_med_expiry: Accelerates ExpiredMedicine_View and NearExpiryMedicine_View date ranges
CREATE INDEX idx_med_expiry ON Medicine(ExpiryDate);

-- idx_med_stock_reorder: Accelerates LowStock_View (WHERE QuantityInStock <= ReorderLevel)
CREATE INDEX idx_med_stock_reorder ON Medicine(QuantityInStock, ReorderLevel);

-- idx_sales_date: Optimizes MonthlySales_View and date-range queries on Sales
CREATE INDEX idx_sales_date ON Sales(SaleDate);

-- idx_purchase_date: Optimizes SupplierWisePurchase_View and purchase analytics
CREATE INDEX idx_purchase_date ON Purchase(PurchaseDate);

-- idx_stocklog_date: Optimizes inventory movement audit history searches
CREATE INDEX idx_stocklog_date ON StockLog(ActionDate);
