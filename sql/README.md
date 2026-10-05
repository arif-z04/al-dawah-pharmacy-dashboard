# Al-Dawah Pharma - Oracle Database Architecture & SQL Deployment Guide

## Overview
This directory contains the production-grade Oracle Database scripts for **Al-Dawah Pharma (Pharmacy Inventory & Sales Management System)**. The database serves as the ultimate source of truth, enforcing business constraints, transactional consistency, auditability, and automated stock calculations via triggers.

---

## Oracle Environment & Version Assumptions
- **Supported Oracle Versions**: Oracle Database 19c, 21c (tested on 21c Express Edition / XE), 23ai.
- **Pluggable / Container Database**: Scripts are designed for standard Oracle CDB/PDB architectures using common application roles (`C##PHARMACY_ADMIN`, `C##PHARMACY_STAFF`) and dedicated application account (`C##PHARMACY_APP`).
- **Default Port & Service**: Port `1521`, Service Name `XE` (or `XEPDB1`).

---

## File Inventory & Execution Order

All scripts must be executed strictly in numeric sequence:

| Step | Script File | Description | Execution Privileges |
|------|-------------|-------------|----------------------|
| 1 | `01_schema.sql` | Drops existing objects safely and creates all 12 core tables | DBA / Schema Owner |
| 2 | `02_constraints_indexes.sql` | Adds foreign keys, check constraints, and performance indexes | DBA / Schema Owner |
| 3 | `03_views.sql` | Compiles the 7 analytical and operational views | DBA / Schema Owner |
| 4 | `04_functions.sql` | Compiles `GET_AVAILABLE_STOCK` and `GET_INVENTORY_VALUE` | DBA / Schema Owner |
| 5 | `05_procedures.sql` | Compiles `ADD_CATEGORY`, `ADD_MEDICINE`, `RECORD_PURCHASE`, `RECORD_SALE` | DBA / Schema Owner |
| 6 | `06_triggers.sql` | Compiles 6 triggers (includes compound triggers for invoice totals) | DBA / Schema Owner |
| 7 | `07_roles_privileges.sql` | Creates roles, grants least-privilege permissions, creates `C##PHARMACY_APP` | DBA (`SYS` or `SYSTEM`) |
| 8 | `08_seed_data.sql` | Populates baseline data (20 users, 20 suppliers, 20 companies, 10 categories, 30 customers, 50 medicines, 50 purchases, 50 purchase details, 50 sales, 50 sales details, 50 stock logs, 20 expiry alerts) | DBA / Schema Owner |
| 9 | `09_reports_queries.sql` | Contains the 8 core optimized analytical reports and bonus queries | Any permitted user |
| 10 | `10_verification.sql` | Automated health-check script verifying tables, views, routines, triggers, and data | Schema Owner / App User |

---

## How to Execute the Scripts

### Step 1: Connect as Administrator
Using `sqlplus` or Oracle SQL Developer:
```bash
sqlplus / as sysdba
# or
sqlplus SYSTEM/<password>@localhost:1521/XE
```

### Step 2: Run Sequential Deployment
In SQL*Plus:
```sql
ALTER SESSION SET CURRENT_SCHEMA = SYSTEM;

@01_schema.sql
@02_constraints_indexes.sql
@03_views.sql
@04_functions.sql
@05_procedures.sql
@06_triggers.sql
@07_roles_privileges.sql
@08_seed_data.sql
```

### Step 3: Run Verification
```sql
@10_verification.sql
```
You will receive an automated `[PASS]` / `[FAIL]` status for every table, view, procedure, function, trigger, constraint, and record count.

---

## Application Connection Details
The backend does **NOT** connect as `SYS` or `SYSTEM`. It connects using the dedicated application credentials:
- **User**: `C##PHARMACY_APP`
- **Password**: Configured securely via `ConnectionStrings:OracleDb` in `appsettings.json` or environment variable `ORACLE_CONNECTION_STRING`.
- **Role**: Uses public synonyms, eliminating any hardcoded `SYSTEM.` prefix in application queries.

---

## Safe Database Reset Procedure
To reset the development database to a clean, seeded state:
1. Re-run `01_schema.sql` through `08_seed_data.sql`.
2. Re-run `10_verification.sql` to confirm that all 12 tables and seed records are restored without errors.
