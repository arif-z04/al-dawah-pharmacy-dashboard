# Al-Dawah Pharma - Pharmacy Inventory & Sales Management System

A production-ready Pharmacy Inventory and Sales Management System built with **Oracle Database**, **ASP.NET Core Web API**, and an independent modern **Frontend**.

---

## 1. Project Overview

**Al-Dawah Pharma** is an enterprise-grade pharmacy inventory, procurement, and point-of-sale management system. The platform streamlines pharmaceutical operations, automates real-time inventory adjustments, audits stock movements, tracks medicine expiration dates, enforces business rules at the database level, and provides rich analytical reporting.

### Core Managed Entities
- **Medicines & Categories**: Complete dosage form classifications and cataloging.
- **Manufacturers & Suppliers**: Pharmaceutical manufacturers and wholesale distributors.
- **Purchases & Sales**: Atomic wholesale ordering and retail dispensing.
- **Inventory & Valuation**: Real-time stock counts and cost/retail valuation.
- **Audit & Movement**: Comprehensive `StockLog` tracking all inventory movements.
- **Expiry Surveillance**: Early warning alerts for near-expiry and expired batches.
- **Analytical Reports**: Views aggregating sales, stock shortfalls, and margins.

---

## 2. Technology Stack

- **Database**: Oracle Database 19c / 21c (XE) / 23ai / 26ai Free (PL/SQL, compound triggers, deterministic functions, stored procedures, analytical views)
- **Backend**: C# / .NET 10.0 ASP.NET Core Web API (Clean Architecture, Inversion of Control, ADO.NET with `Oracle.ManagedDataAccess.Core`, JWT Bearer Authentication, BCrypt)
- **Frontend**: HTML5, Tailwind CSS, Vanilla JavaScript (ES6+), Lucide Icons, Chart.js, Inter Font
- **Testing**: xUnit Integration & Unit Test Suite (.NET 10)

---

## 3. Architecture & Repository Structure

```
AL-Dawah_Pharma/
├── sql/                           # Oracle Database Scripts (Source of Truth)
│   ├── 01_schema.sql              # 12 Core Tables
│   ├── 02_constraints_indexes.sql # FKs, Check Constraints, Performance Indexes
│   ├── 03_views.sql               # 7 Analytical & Operational Views
│   ├── 04_functions.sql           # Deterministic Stock & Valuation Functions
│   ├── 05_procedures.sql          # 4 Stored Procedures (ADD_CATEGORY, ADD_MEDICINE, etc.)
│   ├── 06_triggers.sql            # 6 Automated Triggers (Compound triggers & stock audit)
│   ├── 07_roles_privileges.sql    # RBAC Roles (C##PHARMACY_ADMIN, C##PHARMACY_STAFF, C##PHARMACY_APP)
│   ├── 08_seed_data.sql           # Verified Baseline Seed Data (50 medicines, 50 purchases, etc.)
│   ├── 09_reports_queries.sql     # 8 Core Optimized Reporting Queries
│   ├── 10_verification.sql        # Automated Health-Check Script
│   └── README.md                  # Database Deployment Guide
├── src/
│   ├── Backend/
│   │   ├── Api/                   # Web API Controllers, Middleware, Program.cs
│   │   ├── Application/           # Interfaces, DTOs, Business Exceptions, ApiResponse
│   │   ├── Domain/                # Entities, Domain Enums
│   │   └── Infrastructure/        # Oracle Connection Factory, Repositories, BCrypt, JWT
│   └── Frontend/                  # Independent SPA UI
│       ├── css/styles.css         # Inter Font, Styling & Animations
│       ├── js/api.js              # REST API Client & Error Handler
│       ├── js/auth.js             # Authentication State & Role Authorization
│       ├── js/app.js              # View Controller & Modal Dialogs
│       └── index.html             # Main Responsive Dashboard Shell
├── tests/
│   └── AlDawahPharma.Tests/       # Automated Integration & Unit Tests
├── documentation/                 # Comprehensive 9-Chapter Manual & Beginner's Guide
│   ├── README.md                  # Master Curriculum & Table of Contents
│   ├── 01_absolute_beginner_guide_to_web_and_software.md
│   ├── 02_system_architecture_and_design.md
│   ├── 03_database_deep_dive_and_sql_reference.md
│   ├── 04_backend_csharp_dotnet_code_walkthrough.md
│   ├── 05_frontend_javascript_and_ui_walkthrough.md
│   ├── 06_security_best_practices_and_production_readiness.md
│   ├── 07_setup_guide_windows.md  # Detailed Windows Installation Guide
│   ├── 08_troubleshooting_and_faq.md
│   ├── 09_complete_source_code_annotated_reference.md
│   └── setup-guide.md             # Dedicated Windows Setup Manual
├── docs/                          # Architecture & API References
│   ├── architecture.md            # Clean Layered Architecture Guide
│   ├── database.md                # Oracle Schema, Views, Triggers, Routines
│   ├── api.md                     # REST Endpoints Specification
│   ├── demo-accounts.md           # Development & Evaluation Credentials
│   └── setup.md                   # Step-by-Step Installation Guide
└── README.md
```

---

## 4. Database Setup & Execution Sequence

The Oracle database serves as the ultimate source of truth. All scripts must be run in numeric order:

```bash
# 1. Connect to Oracle Database as SYSDBA
sqlplus / as sysdba
# or: sqlplus SYSTEM/<password>@localhost:1521/FREE
```

```sql
ALTER SESSION SET CURRENT_SCHEMA = SYSTEM;

@sql/01_schema.sql
@sql/02_constraints_indexes.sql
@sql/03_views.sql
@sql/04_functions.sql
@sql/05_procedures.sql
@sql/06_triggers.sql
@sql/07_roles_privileges.sql
@sql/08_seed_data.sql
```

### Automated Verification
```sql
@sql/10_verification.sql
```
Verifies all 12 tables, 7 views, 4 procedures, 2 functions, 6 triggers, role privileges, and foreign keys.

---

## 5. Backend Configuration & Execution

1. Configure connection settings in `src/Backend/Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "OracleDb": "User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;"
     }
   }
   ```
2. Build and run automated tests:
   ```bash
   dotnet test
   ```
3. Start the Web API server:
   ```bash
   dotnet run --project src/Backend/Api
   ```

---

## 6. Accessing the Application & Demo Accounts

Navigate to:
```
http://localhost:5091/index.html
```

Development and demo accounts are documented in [docs/demo-accounts.md](file:///home/noir/Work/AL-Dawah_Pharma/docs/demo-accounts.md):

- **System Administrator**: `admin` / `admin123`
- **Pharmacist / Staff**: `rahim` / `rahim123`

*Passwords are securely hashed using BCrypt in the database.*
