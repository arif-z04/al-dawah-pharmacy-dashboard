-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 04_functions.sql
-- Description: Core PL/SQL deterministic read-only functions
-- Source of Truth: Oracle Database
-- =============================================================================

-- 1. GET_AVAILABLE_STOCK
-- Returns current available QuantityInStock for a given MedicineID
CREATE OR REPLACE FUNCTION GET_AVAILABLE_STOCK
(
    p_medicineid IN NUMBER
)
RETURN NUMBER
AS
    v_stock NUMBER;
BEGIN
    SELECT NVL(QuantityInStock, 0)
    INTO v_stock
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    RETURN v_stock;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/

-- 2. GET_INVENTORY_VALUE
-- Returns current inventory valuation (QuantityInStock * PurchasePrice) for a given MedicineID
CREATE OR REPLACE FUNCTION GET_INVENTORY_VALUE
(
    p_medicineid IN NUMBER
)
RETURN NUMBER
AS
    v_value NUMBER;
BEGIN
    SELECT NVL(QuantityInStock, 0) * NVL(PurchasePrice, 0)
    INTO v_value
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    RETURN v_value;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/
