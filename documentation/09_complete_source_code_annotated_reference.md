# Chapter 9: Complete Source Code Annotated Reference

This chapter serves as a comprehensive annotated directory and code catalog for the entire **Al-Dawah Pharma** codebase. It provides developers, code reviewers, and students with a clear reference of every file, its exact architectural responsibility, and how the components interlock.

---

## 1. Directory Tree & Architecture Map

```
AL-Dawah_Pharma/
│
├── sql/                                    # Oracle Database PL/SQL Scripts (Source of Truth)
│   ├── 01_schema.sql                       # 12 Relational Tables & Sequences
│   ├── 02_constraints_indexes.sql          # Primary, Foreign, Check constraints & B-Tree indexes
│   ├── 03_views.sql                        # 7 Pre-compiled Analytical Views
│   ├── 04_triggers.sql                     # 6 Triggers & Compound Triggers for Stock & Totals
│   ├── 05_procedures.sql                   # 4 Stored Procedures (RECORD_SALE, RECORD_PURCHASE, etc.)
│   ├── 06_functions.sql                    # 2 Functions (GET_AVAILABLE_STOCK, GET_INVENTORY_VALUE)
│   ├── 07_roles_privileges.sql             # Least-privilege C##PHARMACY_APP user & Public Synonyms
│   ├── 08_seed_data.sql                    # Seed data (50 Medicines, BCrypt Users, Purchases, Sales)
│   └── 10_verification.sql                 # Automated verification suite (Tables, Views, Triggers, Procs)
│
├── src/
│   ├── Backend/                            # Clean Multi-Tier ASP.NET Core 10 Web API
│   │   ├── Domain/                         # Pure Domain Entities
│   │   │   ├── Entities/                   # User, Medicine, Sale, Purchase, StockLog, etc.
│   │   │   └── Common/                     # BaseEntity, Value Objects
│   │   │
│   │   ├── Application/                    # Contracts, DTOs & Business Exceptions
│   │   │   ├── Common/                     # ApiResponse<T> standardized envelope
│   │   │   ├── DTOs/                       # DTOs.cs (All Request/Response Models)
│   │   │   ├── Exceptions/                 # NotFoundException, ValidationException, BusinessRuleException
│   │   │   └── Interfaces/                 # Repository & Service Interface Contracts
│   │   │
│   │   ├── Infrastructure/                 # Data Access & External Services
│   │   │   ├── Data/                       # OracleConnectionFactory (Connection Pooling)
│   │   │   ├── Repositories/               # Dapper & ADO.NET Implementations (10 Repositories)
│   │   │   └── Services/                   # BCrypt PasswordHasher & HMAC-SHA256 JwtTokenService
│   │   │
│   │   └── Api/                            # Presentation & Web Layer
│   │       ├── Controllers/                # 10 REST API Controllers (Auth, Sales, Stock, Reports, etc.)
│   │       ├── Middleware/                 # GlobalExceptionMiddleware (Centralized Error Normalization)
│   │       ├── appsettings.json            # Database connection string & JWT secret keys
│   │       └── Program.cs                  # DI Service Registrations & HTTP Pipeline Configuration
│   │
│   └── Frontend/                           # Native Vanilla JavaScript ES6+ Single Page Application (SPA)
│       ├── index.html                      # Layout shell, Sidebar, Top bar, Modal & Toast containers
│       ├── css/
│       │   └── styles.css                  # Custom styling, Scrollbars, @media print invoice styles
│       └── js/
│           ├── api.js                      # Centralized REST client with automatic JWT handling
│           ├── auth.js                     # LocalStorage session manager & Role-Based UI Gating
│           └── app.js                      # SPA Router, Dynamic Table Renderers, POS Cart & Modals
│
├── tests/
│   └── AlDawahPharma.Tests/                # Automated Integration & Unit Test Suite (xUnit)
│       └── UnitTest1.cs                    # 9 Test suites covering DB, Functions, Triggers, JWT, Auth
│
└── documentation/                          # Complete 9-Chapter Manual & Setup Guides
    ├── README.md                           # Master curriculum & index
    ├── 01_absolute_beginner_guide_to_web_and_software.md
    ├── 02_system_architecture_and_design.md
    ├── 03_database_deep_dive_and_sql_reference.md
    ├── 04_backend_csharp_dotnet_code_walkthrough.md
    ├── 05_frontend_javascript_and_ui_walkthrough.md
    ├── 06_security_best_practices_and_production_readiness.md
    ├── 07_setup_guide_windows.md
    ├── 08_troubleshooting_and_faq.md
    ├── 09_complete_source_code_annotated_reference.md
    └── setup-guide.md                      # Dedicated Windows setup manual
```

---

## 2. SQL Scripts Annotated Reference

### `sql/01_schema.sql`
- **Purpose**: Drops existing objects idempotently and creates the 12 core tables and sequences.
- **Key Tables**: `USERS`, `CATEGORIES`, `SUPPLIERS`, `CUSTOMERS`, `MEDICINES`, `PURCHASES`, `PURCHASEDETAILS`, `SALES`, `SALESDETAILS`, `STOCKLOG`, `EXPIRYALERTS`, `AUDITLOG`.
- **Sequences**: All primary keys are backed by individual Oracle sequences (`SEQ_USERS`, `SEQ_MEDICINES`, etc.) starting at 1 with `NOCACHE` to prevent gap fragmentation during unexpected server shutdowns.

### `sql/02_constraints_indexes.sql`
- **Purpose**: Enforces relational data integrity at the database storage engine.
- **Constraints**:
  - `CHECK (UnitPrice >= 0)`, `CHECK (CurrentStock >= 0)`, `CHECK (Quantity > 0)`.
  - `UNIQUE (Username)`, `UNIQUE (CategoryName)`, `UNIQUE (InvoiceNumber)`.
  - Foreign keys with `ON DELETE CASCADE` on child line items (`PURCHASEDETAILS`, `SALESDETAILS`).
- **B-Tree Indexes**: Created on frequently searched columns: `MEDICINES(MedicineName)`, `MEDICINES(GenericName)`, `PURCHASES(InvoiceNumber)`, `SALES(SaleDate)`.

### `sql/03_views.sql`
- **Purpose**: Defines the 7 pre-compiled analytical queries:
  - `VW_CURRENT_STOCK`: Live stock status with stock level evaluation (`In Stock`, `Low Stock`, `Out of Stock`).
  - `VW_EXPIRED_MEDICINES`: Batches where `ExpiryDate < TRUNC(SYSDATE)`.
  - `VW_NEAR_EXPIRY_MEDICINES`: Batches expiring within 90 days (`ExpiryDate <= TRUNC(SYSDATE + 90)`).
  - `VW_LOW_STOCK_MEDICINES`: Medicines where `CurrentStock <= ReorderLevel`.
  - `VW_MONTHLY_SALES_SUMMARY`: Monthly revenue, orders, and discounts.
  - `VW_HIGHEST_SELLING_MEDICINES`: Top medicines sorted by total units sold.
  - `VW_COMPANY_WISE_STOCK`: Stock counts and valuation grouped by pharmaceutical manufacturer.

### `sql/04_triggers.sql`
- **Purpose**: Real-time business logic and automated stock updates.
- **Key Triggers**:
  - `TRG_PURCHASE_TOTAL`: Recalculates `Purchases.TotalAmount` from child line items using a compound trigger.
  - `TRG_SALES_TOTAL`: Recalculates `Sales.TotalAmount` and `Sales.GrandTotal = TotalAmount - Discount` using a compound trigger.
  - `TRG_CHECK_STOCK_BEFORE_SALE`: Row-level trigger with `SELECT ... FOR UPDATE` that aborts with `ORA-20001` if requested quantity > current stock.
  - `TRG_UPDATE_STOCK_SALE`: Decrements `MEDICINES.CurrentStock` and inserts negative movement into `STOCKLOG`.
  - `TRG_UPDATE_STOCK_PURCHASE`: Increments `MEDICINES.CurrentStock`, inserts positive movement into `STOCKLOG`, and records near-expiry alerts.

### `sql/05_procedures.sql` & `sql/06_functions.sql`
- **Stored Procedures**:
  - `ADD_CATEGORY`: Validates and inserts a category.
  - `ADD_MEDICINE`: Inserts a medicine into catalog.
  - `RECORD_PURCHASE`: Creates a purchase order master record and returns the new `PurchaseID`.
  - `RECORD_SALE`: Creates a sales invoice master record and returns the new `SaleID`.
- **Functions**:
  - `GET_AVAILABLE_STOCK(p_medicine_id)`: Fast scalar stock lookup.
  - `GET_INVENTORY_VALUE`: Aggregates `SUM(CurrentStock * PurchasePrice)`.

### `sql/07_roles_privileges.sql`
- **Purpose**: Security separation.
- Configures application user `C##PHARMACY_APP` and role `RL_PHARMACY_APP`.
- Grants `SELECT`, `INSERT`, `UPDATE`, `DELETE` on tables, `EXECUTE` on procedures/functions, and `SELECT` on views/sequences.
- Creates public synonyms for all objects so queries can execute without schema qualifiers.

---

## 3. Backend C# .NET Annotated Reference

### `Api/Program.cs`
Configures:
- Dependency injection for 10 repositories and services.
- JWT Bearer Authentication with HMAC-SHA256 signature validation.
- CORS policy `AllowAll`.
- Static file serving pointing to `src/Frontend`.
- Custom `GlobalExceptionMiddleware` mapping database errors to clean JSON.

### `Api/Controllers/StockController.cs`
- Manages inventory audit logs and manual stock adjustments.
- Maps flexible routes to ensure robust API client compatibility:
  - `GET /api/stock`
  - `GET /api/stock/log`
  - `GET /api/stock-log`
  - `POST /api/stock/adjust`
  - `POST /api/stock-log/adjust`
- Enforces non-zero quantity changes and writes audit trails with user identification.

### `Api/Controllers/SalesController.cs`
- Receives shopping cart payloads from the POS terminal.
- Invokes stored procedure `RECORD_SALE` and inserts line items into `SALESDETAILS`.
- Gracefully handles `OracleException` `ORA-20001` when stock is depleted, returning a helpful HTTP 400 error.

### `Infrastructure/Data/OracleConnectionFactory.cs`
- Reads connection string from `appsettings.json` or environment variable.
- Instantiates `OracleConnection` with connection pooling enabled.

### `Infrastructure/Services/PasswordHasher.cs` & `JwtTokenService.cs`
- `PasswordHasher`: Wraps `BCrypt.Net` with work factor 11.
- `JwtTokenService`: Signs claims (`UserID`, `Username`, `Role`, `FullName`) into 8-hour JWT bearer tokens.

---

## 4. Frontend Annotated Reference

### `src/Frontend/index.html`
- **Sidebar**: Responsive navigation menu that dynamically filters links based on user permissions.
- **Top Header**: User status badge, current role display, notifications bell, and logout button.
- **Content Area (`#content-area`)**: Dynamic injection target where `app.js` renders active modules.
- **Modal System (`#modal-container`)**: Reusable pop-up dialog for forms, confirmations, and invoice receipts.
- **Toast Notifications (`#toast-container`)**: Animated notifications for success, error, and warnings.

### `src/Frontend/js/api.js`
- Unified REST client wrapping standard `fetch()`.
- Automatically injects JWT Bearer tokens from `localStorage`.
- Automatically redirects to login upon HTTP 401 Unauthorized responses.
- Exposes typed methods: `getMedicines()`, `createSale()`, `getStockLogs()`, `getReports()`, etc.

### `src/Frontend/js/auth.js`
- Manages user sessions in `localStorage`.
- Provides helper methods: `isAdmin()`, `isAuthenticated()`, `getUser()`, `getToken()`.

### `src/Frontend/js/app.js`
- Single Page Application router using URL hash changes (`#dashboard`, `#pos`, `#medicines`, `#reports`).
- Contains all UI rendering logic, event listeners, dynamic search filters, and receipt generation.

---

## 5. Automated Test Suite: `tests/AlDawahPharma.Tests`

All 9 integration and unit tests are defined in `UnitTest1.cs`:
1. `TestDatabaseConnection`: Verifies connection pooling to Oracle.
2. `TestCategoryCreation`: Validates category creation and retrieval.
3. `TestSaleRejectionOnInsufficientStock`: Verifies that `ORA-20001` trigger correctly blocks overselling.
4. `TestUserAuthenticationAndJwt`: Verifies BCrypt password verification and JWT token generation.
5. `TestAnalyticalReportViews`: Verifies that all 7 Oracle views execute without SQL syntax errors.
6. `TestMedicineQueryAndFiltering`: Tests search and category filters.
7. `TestPasswordHasherRounds`: Verifies cryptographic salt generation and hash verification.
8. `TestOracleStoredFunctions`: Verifies `GET_AVAILABLE_STOCK` and `GET_INVENTORY_VALUE`.
9. `TestStockLogAuditTrail`: Verifies that stock adjustments correctly record entries in `STOCKLOG`.

To run all tests:
```bash
dotnet test
```

---

*This concludes the complete architecture and code reference for Al-Dawah Pharma.*
