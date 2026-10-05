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
        RAISE_APPLICATION_ERROR(
            -20010,
            'Category ID already exists.'
        );
    END IF;

    -- Check duplicate Category Name
    SELECT COUNT(*)
    INTO v_count
    FROM Category
    WHERE UPPER(CategoryName) = UPPER(p_categoryname);

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20011,
            'Category name already exists.'
        );
    END IF;

    INSERT INTO Category
    (
        CategoryID,
        CategoryName
    )
    VALUES
    (
        p_categoryid,
        p_categoryname
    );

    COMMIT;

    DBMS_OUTPUT.PUT_LINE(
        'Category added successfully.'
    );

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

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
    p_barcode            IN VARCHAR2 DEFAULT NULL,
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
        RAISE_APPLICATION_ERROR(
            -20020,
            'Medicine ID already exists.'
        );
    END IF;

    -- Check duplicate batch number
    SELECT COUNT(*)
    INTO v_count
    FROM Medicine
    WHERE BatchNumber = p_batchnumber;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20021,
            'Batch number already exists.'
        );
    END IF;

    -- Validate price
    IF p_purchaseprice < 0 OR p_sellingprice < 0 THEN
        RAISE_APPLICATION_ERROR(
            -20022,
            'Price cannot be negative.'
        );
    END IF;

    -- Selling price should not be lower than purchase price
    IF p_sellingprice < p_purchaseprice THEN
        RAISE_APPLICATION_ERROR(
            -20023,
            'Selling price cannot be lower than purchase price.'
        );
    END IF;

    -- Validate dates
    IF p_expirydate <= p_manufacturingdate THEN
        RAISE_APPLICATION_ERROR(
            -20024,
            'Expiry date must be after manufacturing date.'
        );
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
        Description
    )
    VALUES
    (
        p_medicineid,
        p_medicinename,
        p_genericname,
        p_categoryid,
        p_companyid,
        p_supplierid,
        p_batchnumber,
        p_purchaseprice,
        p_sellingprice,
        NVL(p_quantityinstock, 0),
        NVL(p_reorderlevel, 10),
        p_manufacturingdate,
        p_expirydate,
        p_barcode,
        p_description
    );

    COMMIT;

    DBMS_OUTPUT.PUT_LINE(
        'Medicine added successfully.'
    );

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/


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
BEGIN

    -- Validate quantity
    IF p_quantity <= 0 THEN
        RAISE_APPLICATION_ERROR(
            -20030,
            'Purchase quantity must be greater than zero.'
        );
    END IF;

    -- Validate unit price
    IF p_unitprice < 0 THEN
        RAISE_APPLICATION_ERROR(
            -20031,
            'Purchase price cannot be negative.'
        );
    END IF;

    -- Check Purchase ID
    SELECT COUNT(*)
    INTO v_count
    FROM Purchase
    WHERE PurchaseID = p_purchaseid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20032,
            'Purchase ID already exists.'
        );
    END IF;

    -- Check medicine
    SELECT COUNT(*)
    INTO v_count
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20033,
            'Medicine does not exist.'
        );
    END IF;

    -- Check supplier
    SELECT COUNT(*)
    INTO v_count
    FROM Supplier
    WHERE SupplierID = p_supplierid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20034,
            'Supplier does not exist.'
        );
    END IF;

    -- Check user
    SELECT COUNT(*)
    INTO v_count
    FROM Users
    WHERE UserID = p_userid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20035,
            'User does not exist.'
        );
    END IF;

    -- Create purchase header
    INSERT INTO Purchase
    (
        PurchaseID,
        SupplierID,
        UserID,
        PurchaseDate,
        TotalAmount
    )
    VALUES
    (
        p_purchaseid,
        p_supplierid,
        p_userid,
        SYSDATE,
        0
    );

    -- Create purchase detail
    INSERT INTO PurchaseDetails
    (
        PurchaseDetailID,
        PurchaseID,
        MedicineID,
        Quantity,
        UnitPrice,
        SubTotal
    )
    VALUES
    (
        (SELECT NVL(MAX(PurchaseDetailID), 0) + 1
         FROM PurchaseDetails),
        p_purchaseid,
        p_medicineid,
        p_quantity,
        p_unitprice,
        NULL
    );

    COMMIT;

    DBMS_OUTPUT.PUT_LINE(
        'Purchase recorded successfully.'
    );

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/

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
BEGIN

    -- Validate quantity
    IF p_quantity <= 0 THEN
        RAISE_APPLICATION_ERROR(
            -20040,
            'Sale quantity must be greater than zero.'
        );
    END IF;

    -- Validate price
    IF p_unitprice < 0 THEN
        RAISE_APPLICATION_ERROR(
            -20041,
            'Selling price cannot be negative.'
        );
    END IF;

    -- Check Sale ID
    SELECT COUNT(*)
    INTO v_count
    FROM Sales
    WHERE SaleID = p_saleid;

    IF v_count > 0 THEN
        RAISE_APPLICATION_ERROR(
            -20042,
            'Sale ID already exists.'
        );
    END IF;

    -- Check medicine
    SELECT COUNT(*)
    INTO v_count
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20043,
            'Medicine does not exist.'
        );
    END IF;

    -- Check customer
    SELECT COUNT(*)
    INTO v_count
    FROM Customer
    WHERE CustomerID = p_customerid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20044,
            'Customer does not exist.'
        );
    END IF;

    -- Check user
    SELECT COUNT(*)
    INTO v_count
    FROM Users
    WHERE UserID = p_userid;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(
            -20045,
            'User does not exist.'
        );
    END IF;

    -- Create sale header
    INSERT INTO Sales
    (
        SaleID,
        CustomerID,
        UserID,
        SaleDate,
        TotalAmount
    )
    VALUES
    (
        p_saleid,
        p_customerid,
        p_userid,
        SYSDATE,
        0
    );

    -- Create sale detail
    INSERT INTO SalesDetails
    (
        SaleDetailID,
        SaleID,
        MedicineID,
        Quantity,
        UnitPrice,
        SubTotal
    )
    VALUES
    (
        (SELECT NVL(MAX(SaleDetailID), 0) + 1
         FROM SalesDetails),
        p_saleid,
        p_medicineid,
        p_quantity,
        p_unitprice,
        NULL
    );

    COMMIT;

    DBMS_OUTPUT.PUT_LINE(
        'Sale recorded successfully.'
    );

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE;
END;
/