# Al-Dawah Pharma - Setup & Deployment Guide

This guide walks through deploying the Oracle Database, configuring the ASP.NET Core backend, running integration tests, and serving the frontend for **Al-Dawah Pharma**.

---

## 1. Prerequisites

- **.NET SDK**: .NET 10.0 (or .NET 8/9 with compatible runtime)
- **Oracle Database**: Oracle Database 19c, 21c (XE), 23ai, or 26ai Free (Docker container supported)
- **Node.js / HTTP server** (optional): The ASP.NET Core API server already hosts the frontend static files natively at `/index.html`.

---

## 2. Oracle Database Setup

### Step 2.1: Run Oracle via Docker / Podman (if not already running)
```bash
docker run -d --name oracle -p 1521:1521 \
  -e ORACLE_PASSWORD=SysPassword2026# \
  container-registry.oracle.com/database/free:latest
```

### Step 2.2: Execute SQL Scripts in Numeric Order
Execute the scripts using `sqlplus`:
```bash
# Copy scripts into container
docker cp ./sql oracle:/tmp/sql

# Execute deployment
docker exec -it oracle bash -c "cd /tmp/sql && sqlplus / as sysdba"
```

In SQL*Plus, run sequentially:
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

### Step 2.3: Verify Installation
```sql
@10_verification.sql
```
This runs an automated health-check verifying that all 12 tables, 7 views, 4 procedures, 2 functions, 6 triggers, role privileges, and seed volumes are in a valid state.

---

## 3. Backend Setup & Configuration

### Step 3.1: Connection Configuration
Inspect or update `src/Backend/Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;"
  },
  "Jwt": {
    "Key": "AlDawahPharmaSecretSuperKey2026!MustBe32BytesLongMin",
    "Issuer": "AlDawahPharmaApi",
    "Audience": "AlDawahPharmaClient",
    "ExpiryMinutes": 720
  }
}
```

Or set the environment variable:
```bash
export ORACLE_CONNECTION_STRING="User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;"
```

### Step 3.2: Run Automated Tests
```bash
dotnet test
```
All unit and integration tests (database connection, password hashing, JWT generation, views, procedures, functions, triggers, and insufficient stock rejection) will execute.

### Step 3.3: Run the Web API
```bash
dotnet run --project src/Backend/Api
```
The API server starts listening on `http://localhost:5091` (or configured port).

---

## 4. Frontend Launch

Open your browser and navigate to:
```
http://localhost:5091/index.html
```
The application will load with real-time data from the Oracle database.

### Demo Credentials
- **Admin**: `admin` / `admin123`
- **Staff**: `rahim` / `rahim123`
(See [demo-accounts.md](file:///home/noir/Work/AL-Dawah_Pharma/docs/demo-accounts.md) for details).
