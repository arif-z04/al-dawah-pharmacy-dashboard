-- users
CREATE TABLE Users (
    UserID NUMBER PRIMARY KEY,
    FullName VARCHAR2(100) NOT NULL,
    Username VARCHAR2(50) UNIQUE NOT NULL,
    Password VARCHAR2(100) NOT NULL,
    Role VARCHAR2(20) NOT NULL
        CHECK (Role IN ('Admin','Pharmacist')),
    Phone VARCHAR2(20),
    Email VARCHAR2(100),
    CreatedAt DATE DEFAULT SYSDATE
);

--Supplier Table
CREATE TABLE Supplier (
    SupplierID NUMBER PRIMARY KEY,
    SupplierName VARCHAR2(100) NOT NULL,
    ContactPerson VARCHAR2(100),
    Phone VARCHAR2(20),
    Email VARCHAR2(100),
    Address VARCHAR2(200),
    CreatedAt DATE DEFAULT SYSDATE
);

--Company Table
CREATE TABLE Company (
    CompanyID NUMBER PRIMARY KEY,
    CompanyName VARCHAR2(100) UNIQUE NOT NULL,
    Address VARCHAR2(200),
    Phone VARCHAR2(20),
    Email VARCHAR2(100)
);

--Category Table

CREATE TABLE Category (
    CategoryID NUMBER PRIMARY KEY,
    CategoryName VARCHAR2(50) UNIQUE NOT NULL
);

--Customer Table

CREATE TABLE Customer (
    CustomerID NUMBER PRIMARY KEY,
    CustomerName VARCHAR2(100) NOT NULL,
    Phone VARCHAR2(20),
    Address VARCHAR2(200)
);

--medicine table
CREATE TABLE Medicine (
    MedicineID NUMBER PRIMARY KEY,

    MedicineName VARCHAR2(100) NOT NULL,
    GenericName VARCHAR2(100),

    CategoryID NUMBER NOT NULL,
    CompanyID NUMBER NOT NULL,
    SupplierID NUMBER NOT NULL,

    BatchNumber VARCHAR2(50) UNIQUE NOT NULL,

    PurchasePrice NUMBER(10,2) NOT NULL
        CHECK (PurchasePrice >= 0),

    SellingPrice NUMBER(10,2) NOT NULL
        CHECK (SellingPrice >= 0),

    QuantityInStock NUMBER DEFAULT 0
        CHECK (QuantityInStock >= 0),

    ReorderLevel NUMBER DEFAULT 10
        CHECK (ReorderLevel >= 0),

    ManufacturingDate DATE NOT NULL,

    ExpiryDate DATE NOT NULL,

    Barcode VARCHAR2(50) UNIQUE,

    Description VARCHAR2(255),

    CreatedAt DATE DEFAULT SYSDATE,

    CONSTRAINT fk_category
        FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),

    CONSTRAINT fk_company
        FOREIGN KEY (CompanyID)
        REFERENCES Company(CompanyID),

    CONSTRAINT fk_supplier
        FOREIGN KEY (SupplierID)
        REFERENCES Supplier(SupplierID),

    CONSTRAINT chk_expiry
        CHECK (ExpiryDate > ManufacturingDate)
);

-- Purchase Table
CREATE TABLE Purchase (
    PurchaseID NUMBER PRIMARY KEY,

    SupplierID NUMBER NOT NULL,

    UserID NUMBER NOT NULL,

    PurchaseDate DATE DEFAULT SYSDATE,

    TotalAmount NUMBER(12,2) DEFAULT 0
        CHECK (TotalAmount >= 0),

    CONSTRAINT fk_purchase_supplier
        FOREIGN KEY (SupplierID)
        REFERENCES Supplier(SupplierID),

    CONSTRAINT fk_purchase_user
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
);

--PurchaseDetails Table
CREATE TABLE PurchaseDetails (

    PurchaseDetailID NUMBER PRIMARY KEY,

    PurchaseID NUMBER NOT NULL,

    MedicineID NUMBER NOT NULL,

    Quantity NUMBER NOT NULL
        CHECK (Quantity > 0),

    UnitPrice NUMBER(10,2) NOT NULL
        CHECK (UnitPrice >= 0),

    SubTotal NUMBER(12,2)
        CHECK (SubTotal >= 0),

    CONSTRAINT fk_pd_purchase
        FOREIGN KEY (PurchaseID)
        REFERENCES Purchase(PurchaseID),

    CONSTRAINT fk_pd_medicine
        FOREIGN KEY (MedicineID)
        REFERENCES Medicine(MedicineID)
);

-- Sales Table
CREATE TABLE Sales (

    SaleID NUMBER PRIMARY KEY,

    CustomerID NUMBER NOT NULL,

    UserID NUMBER NOT NULL,

    SaleDate DATE DEFAULT SYSDATE,

    TotalAmount NUMBER(12,2)
        CHECK (TotalAmount >= 0),

    CONSTRAINT fk_sales_customer
        FOREIGN KEY (CustomerID)
        REFERENCES Customer(CustomerID),

    CONSTRAINT fk_sales_user
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
);

-- SalesDetails Table
CREATE TABLE SalesDetails (

    SaleDetailID NUMBER PRIMARY KEY,

    SaleID NUMBER NOT NULL,

    MedicineID NUMBER NOT NULL,

    Quantity NUMBER NOT NULL
        CHECK (Quantity > 0),

    UnitPrice NUMBER(10,2) NOT NULL
        CHECK (UnitPrice >= 0),

    SubTotal NUMBER(12,2)
        CHECK (SubTotal >= 0),

    CONSTRAINT fk_sd_sale
        FOREIGN KEY (SaleID)
        REFERENCES Sales(SaleID),

    CONSTRAINT fk_sd_medicine
        FOREIGN KEY (MedicineID)
        REFERENCES Medicine(MedicineID)
);

-- StockLog Table

CREATE TABLE StockLog (

    LogID NUMBER PRIMARY KEY,

    MedicineID NUMBER NOT NULL,

    UserID NUMBER NOT NULL,

    ActionType VARCHAR2(20) NOT NULL
        CHECK (ActionType IN ('Purchase','Sale','Adjustment')),

    Quantity NUMBER NOT NULL
        CHECK (Quantity > 0),

    ActionDate DATE DEFAULT SYSDATE,

    Remarks VARCHAR2(255),

    CONSTRAINT fk_stocklog_medicine
        FOREIGN KEY (MedicineID)
        REFERENCES Medicine(MedicineID),

    CONSTRAINT fk_stocklog_user
        FOREIGN KEY (UserID)
        REFERENCES Users(UserID)
); 
-- ExpiryAlert Table
CREATE TABLE ExpiryAlert (

    AlertID NUMBER PRIMARY KEY,

    MedicineID NUMBER NOT NULL,

    AlertDate DATE DEFAULT SYSDATE,

    AlertStatus VARCHAR2(20)
        CHECK (AlertStatus IN ('Near Expiry','Expired')),

    NotificationSent CHAR(1) DEFAULT 'N'
        CHECK (NotificationSent IN ('Y','N')),

    CONSTRAINT fk_alert_medicine
        FOREIGN KEY (MedicineID)
        REFERENCES Medicine(MedicineID)
);