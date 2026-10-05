-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 06_triggers.sql
-- Description: The 6 core triggers for SubTotals, Stock Auditing, and Invoice Totals
-- Source of Truth: Oracle Database
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. TRG_PURCHASE_DETAIL_SUBTOTAL
-- Automatically calculates Quantity * UnitPrice before saving PurchaseDetails
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_PURCHASE_DETAIL_SUBTOTAL
BEFORE INSERT OR UPDATE OF Quantity, UnitPrice
ON PurchaseDetails
FOR EACH ROW
BEGIN
    :NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice;
END;
/

-- -----------------------------------------------------------------------------
-- 2. TRG_PURCHASE_STOCK
-- Automatically increases Medicine.QuantityInStock and creates audit StockLog
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_PURCHASE_STOCK
AFTER INSERT
ON PurchaseDetails
FOR EACH ROW
DECLARE
    v_userid Purchase.UserID%TYPE;
    v_logid  NUMBER;
BEGIN
    -- Look up the user ID responsible for this purchase
    SELECT UserID
    INTO v_userid
    FROM Purchase
    WHERE PurchaseID = :NEW.PurchaseID;

    -- Increase current medicine inventory
    UPDATE Medicine
    SET QuantityInStock = NVL(QuantityInStock, 0) + :NEW.Quantity
    WHERE MedicineID = :NEW.MedicineID;

    -- Generate LogID and record movement in StockLog
    SELECT NVL(MAX(LogID), 0) + 1 INTO v_logid FROM StockLog;

    INSERT INTO StockLog
    (
        LogID,
        MedicineID,
        UserID,
        ActionType,
        Quantity,
        ActionDate,
        Remarks
    )
    VALUES
    (
        v_logid,
        :NEW.MedicineID,
        v_userid,
        'Purchase',
        :NEW.Quantity,
        SYSDATE,
        'Stock increased from Purchase #' || :NEW.PurchaseID
    );
END;
/

-- -----------------------------------------------------------------------------
-- 3. TRG_PURCHASE_TOTAL
-- Automatically updates Purchase.TotalAmount based on SUM(PurchaseDetails.SubTotal).
-- Implemented as an Oracle Compound Trigger to prevent ORA-04091 mutating table errors.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_PURCHASE_TOTAL
FOR INSERT OR UPDATE OR DELETE ON PurchaseDetails
COMPOUND TRIGGER
    TYPE t_purchase_id_list IS TABLE OF Purchase.PurchaseID%TYPE INDEX BY PLS_INTEGER;
    g_purchase_ids t_purchase_id_list;

    AFTER EACH ROW IS
    BEGIN
        IF INSERTING OR UPDATING THEN
            g_purchase_ids(g_purchase_ids.COUNT + 1) := :NEW.PurchaseID;
        ELSE
            g_purchase_ids(g_purchase_ids.COUNT + 1) := :OLD.PurchaseID;
        END IF;
    END AFTER EACH ROW;

    AFTER STATEMENT IS
    BEGIN
        FOR i IN 1 .. g_purchase_ids.COUNT LOOP
            UPDATE Purchase
            SET TotalAmount = (
                SELECT NVL(SUM(SubTotal), 0)
                FROM PurchaseDetails
                WHERE PurchaseID = g_purchase_ids(i)
            )
            WHERE PurchaseID = g_purchase_ids(i);
        END LOOP;
    END AFTER STATEMENT;
END TRG_PURCHASE_TOTAL;
/

-- -----------------------------------------------------------------------------
-- 4. TRG_SALES_DETAIL_SUBTOTAL
-- Automatically calculates Quantity * UnitPrice before saving SalesDetails
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_SALES_DETAIL_SUBTOTAL
BEFORE INSERT OR UPDATE OF Quantity, UnitPrice
ON SalesDetails
FOR EACH ROW
BEGIN
    :NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice;
END;
/

-- -----------------------------------------------------------------------------
-- 5. TRG_SALES_TOTAL
-- Automatically updates Sales.TotalAmount based on SUM(SalesDetails.SubTotal).
-- Implemented as an Oracle Compound Trigger to prevent ORA-04091 mutating table errors.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_SALES_TOTAL
FOR INSERT OR UPDATE OR DELETE ON SalesDetails
COMPOUND TRIGGER
    TYPE t_sale_id_list IS TABLE OF Sales.SaleID%TYPE INDEX BY PLS_INTEGER;
    g_sale_ids t_sale_id_list;

    AFTER EACH ROW IS
    BEGIN
        IF INSERTING OR UPDATING THEN
            g_sale_ids(g_sale_ids.COUNT + 1) := :NEW.SaleID;
        ELSE
            g_sale_ids(g_sale_ids.COUNT + 1) := :OLD.SaleID;
        END IF;
    END AFTER EACH ROW;

    AFTER STATEMENT IS
    BEGIN
        FOR i IN 1 .. g_sale_ids.COUNT LOOP
            UPDATE Sales
            SET TotalAmount = (
                SELECT NVL(SUM(SubTotal), 0)
                FROM SalesDetails
                WHERE SaleID = g_sale_ids(i)
            )
            WHERE SaleID = g_sale_ids(i);
        END LOOP;
    END AFTER STATEMENT;
END TRG_SALES_TOTAL;
/

-- -----------------------------------------------------------------------------
-- 6. TRG_SALE_STOCK
-- Validates that sufficient inventory exists (rejects negative stock via ORA-20001),
-- decreases Medicine.QuantityInStock, and records movement in StockLog.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE TRIGGER TRG_SALE_STOCK
AFTER INSERT
ON SalesDetails
FOR EACH ROW
DECLARE
    v_userid Sales.UserID%TYPE;
    v_stock  Medicine.QuantityInStock%TYPE;
    v_logid  NUMBER;
BEGIN
    -- Query current medicine stock
    SELECT NVL(QuantityInStock, 0)
    INTO v_stock
    FROM Medicine
    WHERE MedicineID = :NEW.MedicineID;

    -- Enforce inventory availability check
    IF v_stock < :NEW.Quantity THEN
        RAISE_APPLICATION_ERROR(
            -20001,
            'Insufficient stock for Medicine ID ' || :NEW.MedicineID ||
            '. Available: ' || v_stock || ', Requested: ' || :NEW.Quantity
        );
    END IF;

    -- Look up the user ID responsible for this sale
    SELECT UserID
    INTO v_userid
    FROM Sales
    WHERE SaleID = :NEW.SaleID;

    -- Decrease inventory
    UPDATE Medicine
    SET QuantityInStock = QuantityInStock - :NEW.Quantity
    WHERE MedicineID = :NEW.MedicineID;

    -- Generate LogID and record movement in StockLog
    SELECT NVL(MAX(LogID), 0) + 1 INTO v_logid FROM StockLog;

    INSERT INTO StockLog
    (
        LogID,
        MedicineID,
        UserID,
        ActionType,
        Quantity,
        ActionDate,
        Remarks
    )
    VALUES
    (
        v_logid,
        :NEW.MedicineID,
        v_userid,
        'Sale',
        :NEW.Quantity,
        SYSDATE,
        'Stock decreased from Sale #' || :NEW.SaleID
    );
END;
/
