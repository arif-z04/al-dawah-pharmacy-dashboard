-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 01_schema.sql
-- Description: Core 12 database tables definition with Primary Keys and columns
-- Source of Truth: Oracle Database
-- =============================================================================

-- Drop child tables first to respect foreign key constraints during recreate
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE ExpiryAlert CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE StockLog CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE SalesDetails CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Sales CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE PurchaseDetails CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Purchase CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Medicine CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Customer CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Category CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Company CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Supplier CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE Users CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF;
END;
/

-- -----------------------------------------------------------------------------
-- 1. USERS Table
-- Stores user accounts, roles, credentials, contact info, and timestamps
-- -----------------------------------------------------------------------------
CREATE TABLE Users (
    UserID      NUMBER PRIMARY KEY,
    FullName    VARCHAR2(100) NOT NULL,
    Username    VARCHAR2(50) UNIQUE NOT NULL,
    Password    VARCHAR2(255) NOT NULL,
    Role        VARCHAR2(20) NOT NULL,
    Phone       VARCHAR2(20),
    Email       VARCHAR2(100),
    CreatedAt   DATE DEFAULT SYSDATE
);

-- -----------------------------------------------------------------------------
-- 2. SUPPLIER Table
-- Stores pharmaceutical distributors, vendors, and contact info
-- -----------------------------------------------------------------------------
CREATE TABLE Supplier (
    SupplierID    NUMBER PRIMARY KEY,
    SupplierName  VARCHAR2(100) NOT NULL,
    ContactPerson VARCHAR2(100),
    Phone         VARCHAR2(20),
    Email         VARCHAR2(100),
    Address       VARCHAR2(200),
    CreatedAt     DATE DEFAULT SYSDATE
);

-- -----------------------------------------------------------------------------
-- 3. COMPANY Table
-- Stores pharmaceutical manufacturing companies
-- -----------------------------------------------------------------------------
CREATE TABLE Company (
    CompanyID   NUMBER PRIMARY KEY,
    CompanyName VARCHAR2(100) UNIQUE NOT NULL,
    Address     VARCHAR2(200),
    Phone       VARCHAR2(20),
    Email       VARCHAR2(100)
);

-- -----------------------------------------------------------------------------
-- 4. CATEGORY Table
-- Stores medicine dosage/drug form categories (Tablet, Syrup, Injection, etc.)
-- -----------------------------------------------------------------------------
CREATE TABLE Category (
    CategoryID   NUMBER PRIMARY KEY,
    CategoryName VARCHAR2(50) UNIQUE NOT NULL
);

-- -----------------------------------------------------------------------------
-- 5. CUSTOMER Table
-- Stores retail customer profiles and contact details
-- -----------------------------------------------------------------------------
CREATE TABLE Customer (
    CustomerID   NUMBER PRIMARY KEY,
    CustomerName VARCHAR2(100) NOT NULL,
    Phone        VARCHAR2(20),
    Address      VARCHAR2(200)
);

-- -----------------------------------------------------------------------------
-- 6. MEDICINE Table
-- Stores core pharmacy medicine catalog, stock levels, pricing, and batch data
-- -----------------------------------------------------------------------------
CREATE TABLE Medicine (
    MedicineID        NUMBER PRIMARY KEY,
    MedicineName      VARCHAR2(100) NOT NULL,
    GenericName       VARCHAR2(100),
    CategoryID        NUMBER NOT NULL,
    CompanyID         NUMBER NOT NULL,
    SupplierID        NUMBER NOT NULL,
    BatchNumber       VARCHAR2(50) UNIQUE NOT NULL,
    PurchasePrice     NUMBER(10,2) NOT NULL,
    SellingPrice      NUMBER(10,2) NOT NULL,
    QuantityInStock   NUMBER DEFAULT 0,
    ReorderLevel      NUMBER DEFAULT 10,
    ManufacturingDate DATE NOT NULL,
    ExpiryDate        DATE NOT NULL,
    Barcode           VARCHAR2(50) UNIQUE,
    Description       VARCHAR2(255),
    CreatedAt         DATE DEFAULT SYSDATE
);

-- -----------------------------------------------------------------------------
-- 7. PURCHASE Table
-- Stores purchase invoice transaction headers from suppliers
-- -----------------------------------------------------------------------------
CREATE TABLE Purchase (
    PurchaseID   NUMBER PRIMARY KEY,
    SupplierID   NUMBER NOT NULL,
    UserID       NUMBER NOT NULL,
    PurchaseDate DATE DEFAULT SYSDATE,
    TotalAmount  NUMBER(12,2) DEFAULT 0
);

-- -----------------------------------------------------------------------------
-- 8. PURCHASEDETAILS Table
-- Stores line-item medicine purchases associated with a purchase transaction
-- -----------------------------------------------------------------------------
CREATE TABLE PurchaseDetails (
    PurchaseDetailID NUMBER PRIMARY KEY,
    PurchaseID       NUMBER NOT NULL,
    MedicineID       NUMBER NOT NULL,
    Quantity         NUMBER NOT NULL,
    UnitPrice        NUMBER(10,2) NOT NULL,
    SubTotal         NUMBER(12,2)
);

-- -----------------------------------------------------------------------------
-- 9. SALES Table
-- Stores customer sales invoice headers
-- -----------------------------------------------------------------------------
CREATE TABLE Sales (
    SaleID      NUMBER PRIMARY KEY,
    CustomerID  NUMBER NOT NULL,
    UserID      NUMBER NOT NULL,
    SaleDate    DATE DEFAULT SYSDATE,
    TotalAmount NUMBER(12,2) DEFAULT 0
);

-- -----------------------------------------------------------------------------
-- 10. SALESDETAILS Table
-- Stores line-item medicine items sold in a sales transaction
-- -----------------------------------------------------------------------------
CREATE TABLE SalesDetails (
    SaleDetailID NUMBER PRIMARY KEY,
    SaleID       NUMBER NOT NULL,
    MedicineID   NUMBER NOT NULL,
    Quantity     NUMBER NOT NULL,
    UnitPrice    NUMBER(10,2) NOT NULL,
    SubTotal     NUMBER(12,2)
);

-- -----------------------------------------------------------------------------
-- 11. STOCKLOG Table
-- Stores audit log of all inventory movements (Purchase, Sale, Adjustment)
-- -----------------------------------------------------------------------------
CREATE TABLE StockLog (
    LogID       NUMBER PRIMARY KEY,
    MedicineID  NUMBER NOT NULL,
    UserID      NUMBER NOT NULL,
    ActionType  VARCHAR2(20) NOT NULL,
    Quantity    NUMBER NOT NULL,
    ActionDate  DATE DEFAULT SYSDATE,
    Remarks     VARCHAR2(255)
);

-- -----------------------------------------------------------------------------
-- 12. EXPIRYALERT Table
-- Stores expiry notifications, dates, and alert statuses
-- -----------------------------------------------------------------------------
CREATE TABLE ExpiryAlert (
    AlertID          NUMBER PRIMARY KEY,
    MedicineID       NUMBER NOT NULL,
    AlertDate        DATE DEFAULT SYSDATE,
    AlertStatus      VARCHAR2(20),
    NotificationSent CHAR(1) DEFAULT 'N'
);
