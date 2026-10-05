# Al-Dawah Pharma - Architecture & System Design Specification

## 1. Architectural Philosophy

Al-Dawah Pharma is built upon a **production-grade Clean Layered Architecture**. The system treats the **Oracle Database as the primary source of truth**, with domain constraints, transactional boundaries, audit logging, and automated calculations enforced at the database level using PL/SQL, views, functions, and compound triggers.

The architecture strictly decouples:
1. **Frontend Presentation Layer** (HTML5, Tailwind CSS, JavaScript ES6+, Lucide Icons)
2. **Backend API Layer** (ASP.NET Core Web API, RESTful endpoints, JWT Authentication, Global Middleware)
3. **Application Layer** (DTOs, Repository Interfaces, Validation, Exceptions, Business logic contracts)
4. **Domain Layer** (Entities, Domain Enums)
5. **Infrastructure Layer** (Oracle.ManagedDataAccess.Core data-access, ADO.NET connection factory, BCrypt password security, JWT token generator)
6. **Persistence Layer** (Oracle Database 19c/21c/23ai/26ai with Pluggable Databases)

```
┌────────────────────────────────────────────────────────┐
│                   Frontend (SPA)                       │
│    HTML5, Tailwind CSS, JavaScript ES6+, Lucide Icons  │
└──────────────────────────┬─────────────────────────────┘
                           │ HTTPS / JSON / Bearer JWT
                           ▼
┌────────────────────────────────────────────────────────┐
│            ASP.NET Core 10 Web API Layer               │
│   Controllers, JWT Bearer Auth, Exception Middleware   │
└──────────────────────────┬─────────────────────────────┘
                           │ Inversion of Control (DI)
                           ▼
┌────────────────────────────────────────────────────────┐
│               Application Business Layer               │
│          Interfaces, DTOs, Business Rules              │
└──────────────────────────┬─────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────┐
│              Infrastructure Layer                      │
│   OracleConnectionFactory, Repositories, BCrypt, JWT   │
└──────────────────────────┬─────────────────────────────┘
                           │ ADO.NET / Oracle Managed Data Access
                           ▼
┌────────────────────────────────────────────────────────┐
│              Oracle Database (Source of Truth)         │
│ 12 Tables, 7 Views, 4 Procedures, 2 Functions, 6 Trgs │
└────────────────────────────────────────────────────────┘
```

---

## 2. Layer Responsibilities

### 2.1 Frontend Presentation Layer
- Fully separated Single Page Application (SPA).
- No mock or hardcoded business datasets in HTML or JS.
- Reactive dashboard KPI cards, Chart.js sales analytics, searchable catalogs, line-item order modals, and receipt views.
- Dynamic API Base URL resolution: works seamlessly when served statically by ASP.NET Core (`http://localhost:5091`) or independently via Live Server / Nginx.

### 2.2 ASP.NET Core Web API
- Exposes clear, restful endpoints following HTTP standards (`GET`, `POST`, `PUT`, `DELETE`).
- **Global Exception Handling Middleware**: Intercepts unhandled errors, domain rule violations, and Oracle database exceptions (`ORA-20001` insufficient stock, `ORA-00001` unique key violations). Transforms them into clean, standardized `ApiResponse<T>` JSON without leaking internal database stack traces.
- **JWT Authentication & Authorization**: Enforces role claims (`Admin`, `Pharmacist`, `Staff`) on sensitive endpoints.

### 2.3 Application & Domain Layers
- **Domain Entities**: Plain C# domain models mirroring Oracle entities without ORM overhead.
- **DTOs**: Encapsulate request/response contracts for isolation between database schema and client presentation.
- **Exceptions**: Custom domain exceptions (`ValidationException`, `BusinessRuleException`, `NotFoundException`, `UnauthorizedException`).

### 2.4 Infrastructure Layer
- **`OracleConnectionFactory`**: Manages connection lifecycles to Oracle CDB/PDB.
- **`PasswordHasher`**: Uses industry-standard **BCrypt** (work factor 11) for password hashing and verification.
- **`JwtTokenService`**: Signs standard HMAC-SHA256 JWT tokens containing user claims, role, and expiration.
- **Repositories**: Execute optimized relational SQL queries, call PL/SQL stored procedures (`ADD_MEDICINE`, `RECORD_PURCHASE`, `RECORD_SALE`), and evaluate database functions (`GET_AVAILABLE_STOCK`, `GET_INVENTORY_VALUE`).

---

## 3. Transaction Atomicity & Stock Integrity

### Multi-Item Transactions
A purchase or sale transaction involving multiple medicines is strictly atomic:
1. `BeginTransaction()` begins an Oracle transaction.
2. The invoice header is inserted (`Purchase` or `Sales`).
3. For each detail item, `PurchaseDetails` or `SalesDetails` is inserted.
4. **Oracle Trigger Execution**:
   - `TRG_PURCHASE_STOCK`: Automatically increments `Medicine.QuantityInStock` and inserts into `StockLog`.
   - `TRG_PURCHASE_TOTAL`: Compound trigger calculates `SUM(SubTotal)` and updates `Purchase.TotalAmount`.
   - `TRG_SALE_STOCK`: Verifies that `QuantityInStock >= :NEW.Quantity`. If stock is insufficient, immediately raises `ORA-20001`!
   - `TRG_SALES_TOTAL`: Compound trigger calculates `SUM(SubTotal)` and updates `Sales.TotalAmount`.
5. If any detail fails (e.g. stock shortfall), the entire transaction rolls back via `tx.Rollback()`. No orphan records or partial stock deductions are permitted.
