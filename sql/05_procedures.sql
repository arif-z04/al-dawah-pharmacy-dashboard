-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 05_procedures.sql
-- Description: Core PL/SQL Stored Procedures for Category, Medicine, Purchase & Sales
-- Source of Truth: Oracle Database
-- NOTE: Procedures do NOT duplicate stock/total logic handled by Triggers.
-- =============================================================================

SET DEFINE OFF;

-- -----------------------------------------------------------------------------
-- 1. ADD_CATEGORY
-- Inserts a new category with duplicate validation
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE ADD_CATEGORY
(
    p_categoryid   IN NUMBER,
    p_categoryname IN VARCHAR2
)
AS
    v_count NUMBER;
BEGIN
    -- Check duplicate Category ID
    SELECT COUNT(*)
    INTO v_count
    FROM Category
    WHERE CategoryID = p_categoryid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20010, 'Category ID ' || p_categoryid || ' already exists.');
    END IF;

    -- Check duplicate Category Name
    SELECT COUNT(*)
    INTO v_count
    FROM Category
    WHERE UPPER(CategoryName) = UPPER(TRIM(p_categoryname));

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20011, 'Category name ''' || p_categoryname || ''' already exists.');
    END IF;

    INSERT INTO Category (CategoryID, CategoryName)
    VALUES (p_categoryid, TRIM(p_categoryname));

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

-- -----------------------------------------------------------------------------
-- 2. ADD_MEDICINE
-- Inserts a new medicine with complete business validation
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE ADD_MEDICINE
(
    p_medicineid        IN NUMBER,
    p_medicinename      IN VARCHAR2,
    p_genericname       IN VARCHAR2,
    p_categoryid        IN NUMBER,
    p_companyid         IN NUMBER,
    p_supplierid        IN NUMBER,
    p_batchnumber       IN VARCHAR2,
    p_purchaseprice     IN NUMBER,
    p_sellingprice      IN NUMBER,
    p_quantityinstock   IN NUMBER,
    p_reorderlevel      IN NUMBER,
    p_manufacturingdate IN DATE,
    p_expirydate        IN DATE,
    p_barcode           IN VARCHAR2 DEFAULT NULL,
    p_description       IN VARCHAR2 DEFAULT NULL
)
AS
    v_count NUMBER;
BEGIN
    -- Check duplicate Medicine ID
    SELECT COUNT(*)
    INTO v_count
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20020, 'Medicine ID ' || p_medicineid || ' already exists.');
    END IF;

    -- Check duplicate batch number
    SELECT COUNT(*)
    INTO v_count
    FROM Medicine
    WHERE UPPER(BatchNumber) = UPPER(TRIM(p_batchnumber));

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20021, 'Batch number ''' || p_batchnumber || ''' already exists.');
    END IF;

    -- Validate foreign keys
    SELECT COUNT(*) INTO v_count FROM Category WHERE CategoryID = p_categoryid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20025, 'CategoryID ' || p_categoryid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Company WHERE CompanyID = p_companyid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20026, 'CompanyID ' || p_companyid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Supplier WHERE SupplierID = p_supplierid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20027, 'SupplierID ' || p_supplierid || ' does not exist.');
    END IF;

    -- Validate pricing
    IF p_purchaseprice < 0 OR p_sellingprice < 0 THEN
        RAISE_APPLICATION_ERROR(-20022, 'Price cannot be negative.');
    END IF;

    IF p_sellingprice < p_purchaseprice THEN
        RAISE_APPLICATION_ERROR(-20023, 'Selling price cannot be lower than purchase price.');
    END IF;

    -- Validate dates
    IF p_expirydate <= p_manufacturingdate THEN
        RAISE_APPLICATION_ERROR(-20024, 'Expiry date must be after manufacturing date.');
    END IF;

    INSERT INTO Medicine
    (
        MedicineID,
        MedicineName,
        GenericName,
        CategoryID,
        CompanyID,
        SupplierID,
        BatchNumber,
        PurchasePrice,
        SellingPrice,
        QuantityInStock,
        ReorderLevel,
        ManufacturingDate,
        ExpiryDate,
        Barcode,
        Description,
        CreatedAt
    )
    VALUES
    (
        p_medicineid,
        TRIM(p_medicinename),
        TRIM(p_genericname),
        p_categoryid,
        p_companyid,
        p_supplierid,
        TRIM(p_batchnumber),
        p_purchaseprice,
        p_sellingprice,
        NVL(p_quantityinstock, 0),
        NVL(p_reorderlevel, 10),
        p_manufacturingdate,
        p_expirydate,
        TRIM(p_barcode),
        TRIM(p_description),
        SYSDATE
    );

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

-- -----------------------------------------------------------------------------
-- 3. RECORD_PURCHASE
-- Records purchase header & detail.
-- NOTE: SubTotal, Stock update, and Purchase TotalAmount are automatically
-- managed by TRG_PURCHASE_DETAIL_SUBTOTAL, TRG_PURCHASE_STOCK, and TRG_PURCHASE_TOTAL.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE RECORD_PURCHASE
(
    p_purchaseid IN NUMBER,
    p_supplierid IN NUMBER,
    p_userid     IN NUMBER,
    p_medicineid IN NUMBER,
    p_quantity   IN NUMBER,
    p_unitprice  IN NUMBER
)
AS
    v_count NUMBER;
    v_detail_id NUMBER;
BEGIN
    -- Validate quantity
    IF p_quantity <= 0 THEN
        RAISE_APPLICATION_ERROR(-20030, 'Purchase quantity must be greater than zero.');
    END IF;

    -- Validate unit price
    IF p_unitprice < 0 THEN
        RAISE_APPLICATION_ERROR(-20031, 'Purchase unit price cannot be negative.');
    END IF;

    -- Check duplicate Purchase ID
    SELECT COUNT(*)
    INTO v_count
    FROM Purchase
    WHERE PurchaseID = p_purchaseid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20032, 'Purchase ID ' || p_purchaseid || ' already exists.');
    END IF;

    -- Check foreign keys
    SELECT COUNT(*) INTO v_count FROM Supplier WHERE SupplierID = p_supplierid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20034, 'Supplier ID ' || p_supplierid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Users WHERE UserID = p_userid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20035, 'User ID ' || p_userid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Medicine WHERE MedicineID = p_medicineid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20033, 'Medicine ID ' || p_medicineid || ' does not exist.');
    END IF;

    -- Generate detail ID
    SELECT NVL(MAX(PurchaseDetailID), 0) + 1
    INTO v_detail_id
    FROM PurchaseDetails;

    -- 1. Insert header (TotalAmount initialized to 0, updated by trigger)
    INSERT INTO Purchase (PurchaseID, SupplierID, UserID, PurchaseDate, TotalAmount)
    VALUES (p_purchaseid, p_supplierid, p_userid, SYSDATE, 0);

    -- 2. Insert detail (Triggers calculate SubTotal, increase Medicine stock, update TotalAmount, log StockLog)
    INSERT INTO PurchaseDetails (PurchaseDetailID, PurchaseID, MedicineID, Quantity, UnitPrice, SubTotal)
    VALUES (v_detail_id, p_purchaseid, p_medicineid, p_quantity, p_unitprice, NULL);

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

-- -----------------------------------------------------------------------------
-- 4. RECORD_SALE
-- Records sales header & detail.
-- NOTE: SubTotal, Stock decrease & check, and Sales TotalAmount are automatically
-- managed by TRG_SALES_DETAIL_SUBTOTAL, TRG_SALE_STOCK, and TRG_SALES_TOTAL.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE RECORD_SALE
(
    p_saleid     IN NUMBER,
    p_customerid IN NUMBER,
    p_userid     IN NUMBER,
    p_medicineid IN NUMBER,
    p_quantity   IN NUMBER,
    p_unitprice  IN NUMBER
)
AS
    v_count NUMBER;
    v_detail_id NUMBER;
BEGIN
    -- Validate quantity
    IF p_quantity <= 0 THEN
        RAISE_APPLICATION_ERROR(-20040, 'Sale quantity must be greater than zero.');
    END IF;

    -- Validate unit price
    IF p_unitprice < 0 THEN
        RAISE_APPLICATION_ERROR(-20041, 'Selling price cannot be negative.');
    END IF;

    -- Check duplicate Sale ID
    SELECT COUNT(*)
    INTO v_count
    FROM Sales
    WHERE SaleID = p_saleid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(-20042, 'Sale ID ' || p_saleid || ' already exists.');
    END IF;

    -- Check foreign keys
    SELECT COUNT(*) INTO v_count FROM Customer WHERE CustomerID = p_customerid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20044, 'Customer ID ' || p_customerid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Users WHERE UserID = p_userid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20045, 'User ID ' || p_userid || ' does not exist.');
    END IF;

    SELECT COUNT(*) INTO v_count FROM Medicine WHERE MedicineID = p_medicineid;
    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20043, 'Medicine ID ' || p_medicineid || ' does not exist.');
    END IF;

    -- Generate detail ID
    SELECT NVL(MAX(SaleDetailID), 0) + 1
    INTO v_detail_id
    FROM SalesDetails;

    -- 1. Insert header (TotalAmount initialized to 0, updated by trigger)
    INSERT INTO Sales (SaleID, CustomerID, UserID, SaleDate, TotalAmount)
    VALUES (p_saleid, p_customerid, p_userid, SYSDATE, 0);

    -- 2. Insert detail (Triggers calculate SubTotal, verify & decrease Medicine stock, update TotalAmount, log StockLog)
    INSERT INTO SalesDetails (SaleDetailID, SaleID, MedicineID, Quantity, UnitPrice, SubTotal)
    VALUES (v_detail_id, p_saleid, p_medicineid, p_quantity, p_unitprice, NULL);

    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/
