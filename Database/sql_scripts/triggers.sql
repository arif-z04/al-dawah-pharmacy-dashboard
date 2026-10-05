--Automatically calculate PurchaseDetails SubTotal
CREATE OR REPLACE TRIGGER trg_purchase_detail_subtotal
BEFORE INSERT OR UPDATE OF Quantity, UnitPrice
ON PurchaseDetails
FOR EACH ROW
BEGIN
    :NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice;
END;
/

CREATE OR REPLACE TRIGGER trg_sales_detail_subtotal
BEFORE INSERT OR UPDATE OF Quantity, UnitPrice
ON SalesDetails
FOR EACH ROW
BEGIN
    :NEW.SubTotal := :NEW.Quantity * :NEW.UnitPrice;
END;
/

CREATE OR REPLACE TRIGGER trg_purchase_stock
AFTER INSERT
ON PurchaseDetails
FOR EACH ROW
DECLARE
    v_userid Purchase.UserID%TYPE;
BEGIN

    -- Get the user who made the purchase
    SELECT UserID
    INTO v_userid
    FROM Purchase
    WHERE PurchaseID = :NEW.PurchaseID;

    -- Increase medicine stock
    UPDATE Medicine
    SET QuantityInStock = NVL(QuantityInStock, 0) + :NEW.Quantity
    WHERE MedicineID = :NEW.MedicineID;

    -- Record stock movement
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
        (SELECT NVL(MAX(LogID), 0) + 1 FROM StockLog),
        :NEW.MedicineID,
        v_userid,
        'Purchase',
        :NEW.Quantity,
        SYSDATE,
        'Stock increased from purchase'
    );

END;
/
CREATE OR REPLACE TRIGGER trg_sale_stock
AFTER INSERT
ON SalesDetails
FOR EACH ROW
DECLARE
    v_userid Sales.UserID%TYPE;
    v_stock  Medicine.QuantityInStock%TYPE;
BEGIN

    -- Get current stock
    SELECT NVL(QuantityInStock, 0)
    INTO v_stock
    FROM Medicine
    WHERE MedicineID = :NEW.MedicineID;

    -- Check whether enough medicine exists
    IF v_stock < :NEW.Quantity THEN
        RAISE_APPLICATION_ERROR(
            -20001,
            'Insufficient stock for Medicine ID ' || :NEW.MedicineID
        );
    END IF;

    -- Get the user who made the sale
    SELECT UserID
    INTO v_userid
    FROM Sales
    WHERE SaleID = :NEW.SaleID;

    -- Decrease stock
    UPDATE Medicine
    SET QuantityInStock = QuantityInStock - :NEW.Quantity
    WHERE MedicineID = :NEW.MedicineID;

    -- Record stock movement
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
        (SELECT NVL(MAX(LogID), 0) + 1 FROM StockLog),
        :NEW.MedicineID,
        v_userid,
        'Sale',
        :NEW.Quantity,
        SYSDATE,
        'Stock decreased from sale'
    );

END;
/

CREATE OR REPLACE TRIGGER trg_purchase_total
AFTER INSERT OR UPDATE OR DELETE
ON PurchaseDetails
FOR EACH ROW
BEGIN

    UPDATE Purchase
    SET TotalAmount =
        (
            SELECT NVL(SUM(SubTotal), 0)
            FROM PurchaseDetails
            WHERE PurchaseID =
                NVL(:NEW.PurchaseID, :OLD.PurchaseID)
        )
    WHERE PurchaseID =
        NVL(:NEW.PurchaseID, :OLD.PurchaseID);

END;
/
CREATE OR REPLACE TRIGGER trg_sales_total
AFTER INSERT OR UPDATE OR DELETE
ON SalesDetails
FOR EACH ROW
BEGIN

    UPDATE Sales
    SET TotalAmount =
        (
            SELECT NVL(SUM(SubTotal), 0)
            FROM SalesDetails
            WHERE SaleID =
                NVL(:NEW.SaleID, :OLD.SaleID)
        )
    WHERE SaleID =
        NVL(:NEW.SaleID, :OLD.SaleID);

END;
/