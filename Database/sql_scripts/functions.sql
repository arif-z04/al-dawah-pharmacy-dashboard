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

CREATE OR REPLACE FUNCTION GET_INVENTORY_VALUE
(
    p_medicineid IN NUMBER
)
RETURN NUMBER
AS
    v_value NUMBER;
BEGIN

    SELECT
        NVL(QuantityInStock, 0) * NVL(PurchasePrice, 0)
    INTO v_value
    FROM Medicine
    WHERE MedicineID = p_medicineid;

    RETURN v_value;

EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN 0;
END;
/