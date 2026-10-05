-- =============================================================================
-- Al-Dawah Pharma - Pharmacy Inventory & Sales Management System
-- Script: 10_verification.sql
-- Description: Automated Database Verification and Health Check Script
-- Source of Truth: Oracle Database
-- =============================================================================

SET SERVEROUTPUT ON SIZE UNLIMITED;
SET LINESIZE 200;
SET PAGESIZE 100;

PROMPT =============================================================================
PROMPT AL-DAWAH PHARMA: DATABASE INTEGRITY AND HEALTH CHECK
PROMPT =============================================================================

DECLARE
    v_errors NUMBER := 0;
    v_warnings NUMBER := 0;
    v_count NUMBER;

    PROCEDURE report_check(p_item VARCHAR2, p_status BOOLEAN, p_details VARCHAR2 DEFAULT NULL) IS
    BEGIN
        IF p_status THEN
            DBMS_OUTPUT.PUT_LINE('[PASS] ' || RPAD(p_item, 45) || ' OK ' || NVL(p_details, ''));
        ELSE
            DBMS_OUTPUT.PUT_LINE('[FAIL] ' || RPAD(p_item, 45) || ' FAILED ' || NVL(p_details, ''));
            v_errors := v_errors + 1;
        END IF;
    END;

BEGIN
    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 1: CORE 12 TABLES CHECK ---');
    FOR r IN (
        SELECT column_value AS tbl FROM TABLE(sys.odcivarchar2list(
            'USERS', 'SUPPLIER', 'COMPANY', 'CATEGORY', 'CUSTOMER', 'MEDICINE',
            'PURCHASE', 'PURCHASEDETAILS', 'SALES', 'SALESDETAILS', 'STOCKLOG', 'EXPIRYALERT'
        ))
    ) LOOP
        SELECT COUNT(*) INTO v_count
        FROM all_tables
        WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') AND table_name = r.tbl;
        
        report_check('Table ' || r.tbl, v_count = 1);
    END LOOP;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 2: 7 VIEWS STATUS CHECK ---');
    FOR r IN (
        SELECT column_value AS vw FROM TABLE(sys.odcivarchar2list(
            'EXPIREDMEDICINE_VIEW', 'NEAREXPIRYMEDICINE_VIEW', 'LOWSTOCK_VIEW',
            'MONTHLYSALES_VIEW', 'INVENTORYVALUE_VIEW', 'COMPANYWISESTOCK_VIEW',
            'SUPPLIERWISEPURCHASE_VIEW'
        ))
    ) LOOP
        SELECT COUNT(*) INTO v_count
        FROM all_objects
        WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
          AND object_type = 'VIEW' AND object_name = r.vw AND status = 'VALID';

        report_check('View ' || r.vw, v_count = 1);
    END LOOP;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 3: 4 PROCEDURES STATUS CHECK ---');
    FOR r IN (
        SELECT column_value AS prc FROM TABLE(sys.odcivarchar2list(
            'ADD_CATEGORY', 'ADD_MEDICINE', 'RECORD_PURCHASE', 'RECORD_SALE'
        ))
    ) LOOP
        SELECT COUNT(*) INTO v_count
        FROM all_objects
        WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
          AND object_type = 'PROCEDURE' AND object_name = r.prc AND status = 'VALID';

        report_check('Procedure ' || r.prc, v_count = 1);
    END LOOP;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 4: 2 FUNCTIONS STATUS CHECK ---');
    FOR r IN (
        SELECT column_value AS fnc FROM TABLE(sys.odcivarchar2list(
            'GET_AVAILABLE_STOCK', 'GET_INVENTORY_VALUE'
        ))
    ) LOOP
        SELECT COUNT(*) INTO v_count
        FROM all_objects
        WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
          AND object_type = 'FUNCTION' AND object_name = r.fnc AND status = 'VALID';

        report_check('Function ' || r.fnc, v_count = 1);
    END LOOP;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 5: 6 TRIGGERS STATUS CHECK ---');
    FOR r IN (
        SELECT column_value AS trg FROM TABLE(sys.odcivarchar2list(
            'TRG_PURCHASE_DETAIL_SUBTOTAL', 'TRG_PURCHASE_STOCK', 'TRG_PURCHASE_TOTAL',
            'TRG_SALES_DETAIL_SUBTOTAL', 'TRG_SALES_TOTAL', 'TRG_SALE_STOCK'
        ))
    ) LOOP
        SELECT COUNT(*) INTO v_count
        FROM all_triggers
        WHERE owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
          AND trigger_name = r.trg AND status = 'ENABLED';

        report_check('Trigger ' || r.trg, v_count = 1);
    END LOOP;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 6: SEED DATA VOLUME AUDIT ---');
    DECLARE
        v_rows NUMBER;
        PROCEDURE check_count(p_name VARCHAR2, p_sql VARCHAR2, p_min NUMBER) IS
        BEGIN
            EXECUTE IMMEDIATE p_sql INTO v_rows;
            report_check('Seed Data: ' || p_name, v_rows >= p_min, '(Found: ' || v_rows || ', Min: ' || p_min || ')');
        END;
    BEGIN
        check_count('USERS', 'SELECT COUNT(*) FROM Users', 20);
        check_count('SUPPLIER', 'SELECT COUNT(*) FROM Supplier', 20);
        check_count('COMPANY', 'SELECT COUNT(*) FROM Company', 20);
        check_count('CATEGORY', 'SELECT COUNT(*) FROM Category', 10);
        check_count('CUSTOMER', 'SELECT COUNT(*) FROM Customer', 30);
        check_count('MEDICINE', 'SELECT COUNT(*) FROM Medicine', 50);
        check_count('PURCHASE', 'SELECT COUNT(*) FROM Purchase', 50);
        check_count('PURCHASEDETAILS', 'SELECT COUNT(*) FROM PurchaseDetails', 50);
        check_count('SALES', 'SELECT COUNT(*) FROM Sales', 50);
        check_count('SALESDETAILS', 'SELECT COUNT(*) FROM SalesDetails', 50);
        check_count('STOCKLOG', 'SELECT COUNT(*) FROM StockLog', 50);
        check_count('EXPIRYALERT', 'SELECT COUNT(*) FROM ExpiryAlert', 20);
    END;

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '--- SECTION 7: ORPHAN / REFERENTIAL INTEGRITY CHECK ---');
    -- Check orphan PurchaseDetails
    SELECT COUNT(*) INTO v_count FROM PurchaseDetails pd WHERE NOT EXISTS (SELECT 1 FROM Purchase p WHERE p.PurchaseID = pd.PurchaseID);
    report_check('Orphan PurchaseDetails -> Purchase', v_count = 0, '(Found: ' || v_count || ')');

    -- Check orphan SalesDetails
    SELECT COUNT(*) INTO v_count FROM SalesDetails sd WHERE NOT EXISTS (SELECT 1 FROM Sales s WHERE s.SaleID = sd.SaleID);
    report_check('Orphan SalesDetails -> Sales', v_count = 0, '(Found: ' || v_count || ')');

    -- Check orphan Medicine Category
    SELECT COUNT(*) INTO v_count FROM Medicine m WHERE NOT EXISTS (SELECT 1 FROM Category c WHERE c.CategoryID = m.CategoryID);
    report_check('Orphan Medicine -> Category', v_count = 0, '(Found: ' || v_count || ')');

    -- Check orphan Medicine Company
    SELECT COUNT(*) INTO v_count FROM Medicine m WHERE NOT EXISTS (SELECT 1 FROM Company cp WHERE cp.CompanyID = m.CompanyID);
    report_check('Orphan Medicine -> Company', v_count = 0, '(Found: ' || v_count || ')');

    -- Check orphan Medicine Supplier
    SELECT COUNT(*) INTO v_count FROM Medicine m WHERE NOT EXISTS (SELECT 1 FROM Supplier s WHERE s.SupplierID = m.SupplierID);
    report_check('Orphan Medicine -> Supplier', v_count = 0, '(Found: ' || v_count || ')');

    DBMS_OUTPUT.PUT_LINE(CHR(10) || '=============================================================================');
    IF v_errors = 0 THEN
        DBMS_OUTPUT.PUT_LINE('VERIFICATION RESULT: ALL TESTS PASSED! DATABASE READY FOR PRODUCTION.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('VERIFICATION RESULT: ' || v_errors || ' CHECKS FAILED. PLEASE REVIEW LOGS ABOVE.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('=============================================================================');
END;
/
