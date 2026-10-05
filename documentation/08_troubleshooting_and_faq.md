# Chapter 8: Troubleshooting, Error Reference & FAQ

This chapter is an exhaustive diagnostic manual and encyclopedia of solutions for any error, exception, or unexpected behavior encountered while developing, testing, or operating the **Al-Dawah Pharmacy Management System**.

---

## 1. Oracle Database Error Catalog

### 1. `ORA-12541: TNS:no listener`
- **Cause**: The application is trying to connect to port 1521, but the Oracle database listener process has not started yet or the container is still booting.
- **Diagnosis**:
  ```bash
  docker logs oracle
  ```
- **Solution**:
  Oracle 23c / 26ai Free takes 1 to 3 minutes to initialize its data files on first launch. Wait until `docker logs oracle` prints:
  `DATABASE IS READY TO USE!`.
  If you are running natively on Windows, open `services.msc` and verify that the `OracleOraDB21Home1TNSListener` service is set to **Running**.

---

### 2. `ORA-01017: invalid username/password; logon denied`
- **Cause**: The username, password, or connection string does not match the database credentials.
- **Common Mistakes**:
  - In Oracle 12c+ / 21c / 23c multitenant architecture, common users created in the root container (`CDB$ROOT`) must start with the prefix `C##` (e.g., `C##PHARMACY_APP`).
  - Oracle passwords are case-sensitive.
- **Solution**:
  Test logging in using `sqlplus`:
  ```bash
  sqlplus C##PHARMACY_APP/PharmacyApp2026#@localhost:1521/FREE
  ```
  If password expired or needs reset:
  ```sql
  ALTER USER C##PHARMACY_APP IDENTIFIED BY PharmacyApp2026#;
  ```

---

### 3. `ORA-00942: table or view does not exist`
- **Cause**: The table was not created, the user lacks `SELECT` privileges, or public synonyms are missing.
- **Solution**:
  When logging in as `C##PHARMACY_APP`, verify public synonyms exist:
  ```sql
  SELECT table_name FROM user_synonyms;
  ```
  If synonyms are missing, execute `sql/07_roles_privileges.sql` as `SYSTEM`.

---

### 4. `ORA-20001: Insufficient stock for Medicine ID ...`
- **Cause**: This is **not a bug**; it is our safety trigger `TRG_CHECK_STOCK_BEFORE_SALE` working as designed! It fired because a cashier attempted to sell more units of a drug than physical inventory holds.
- **How It Appears in API**:
  The ASP.NET Core `SalesController` catches this exception and converts it into a clean HTTP 400 Bad Request:
  ```json
  {
    "success": false,
    "message": "Cannot complete sale: Insufficient stock available for one or more medicines.",
    "data": null
  }
  ```

---

### 5. `ORA-04091: table is mutating, trigger/function may not see it`
- **Cause**: A row-level trigger on a child table (like `SALESDETAILS`) attempted to execute a `SELECT SUM(...)` or `UPDATE` on the parent table (`SALES`) while the row was still being inserted.
- **Solution**:
  Never use a traditional `FOR EACH ROW` trigger for parent aggregations. In Al-Dawah Pharma, we use **Compound Triggers** (`TRG_SALES_TOTAL` and `TRG_PURCHASE_TOTAL`) which collect IDs during row events and perform calculations in the `AFTER STATEMENT` phase once the table is no longer mutating.

---

### 6. `ORA-00001: unique constraint violated`
- **Cause**: An attempt to insert a duplicate value into a column protected by a `UNIQUE` constraint (e.g., `USERS(Username)`, `CATEGORIES(CategoryName)`, `PURCHASES(InvoiceNumber)`).
- **Solution**: Check existing records before inserting or use a distinct invoice identifier.

---

## 2. ASP.NET Core & C# Error Catalog

### 1. `AmbiguousMatchException: The request matched multiple endpoints`
- **Cause**: Two or more action methods or route attributes in a controller match the identical HTTP method and URL path.
- **Example That Caused the Stock Log 404/500 Issue**:
  ```csharp
  // BAD: Both resolve to /api/stock/log!
  [HttpGet("log")]
  [HttpGet("/api/stock/log")] 
  public IActionResult GetLogs() ...
  ```
- **Solution**:
  Use non-conflicting templates:
  ```csharp
  [HttpGet]                 // Matches /api/stock
  [HttpGet("log")]          // Matches /api/stock/log
  [HttpGet("/api/stock-log")] // Matches /api/stock-log
  ```

---

### 2. `IDX10501: Signature validation failed. Unable to match keys`
- **Cause**: The incoming JWT token was signed with a different secret key than the one configured in `appsettings.json`, or the token string was truncated.
- **Solution**:
  Verify that the `Jwt:Key` in `appsettings.json` is at least 256 bits (32 characters long) and matches between generation and validation. Clear the browser's `localStorage` and log in again to generate a fresh token.

---

### 3. `Cross-Origin Request Blocked (CORS: Network Error)`
- **Cause**: The web browser prevented a frontend script running on one origin (e.g., `http://127.0.0.1:5500`) from accessing the API on another origin (e.g., `http://localhost:5091`) because CORS headers were missing.
- **Solution**:
  In `Program.cs`, ensure `app.UseCors("AllowAll")` is placed **before** `app.UseAuthentication()` and `app.UseAuthorization()`.

---

## 3. Frontend & Browser Error Catalog

### 1. `TypeError: Failed to fetch`
- **Cause**:
  1. The ASP.NET Core backend server is not running.
  2. The server crashed due to an unhandled startup exception.
  3. The browser is trying to connect to the wrong port.
- **Diagnosis**:
  Check if `http://localhost:5091/api/dashboard/summary` is reachable in your browser or via curl. If not, start the backend with `dotnet run --project src/Backend/Api --launch-profile http`.

---

### 2. `Uncaught ReferenceError: lucide is not defined`
- **Cause**: The computer running the browser has no internet connection, and the Lucide Icons CDN script could not load.
- **Solution**:
  Verify internet access, or download `lucide.min.js` locally and reference it from `src/Frontend/js/vendor/lucide.min.js`.

---

## 4. Frequently Asked Questions (FAQ)

### Q1: Why did you choose Oracle Database instead of MySQL, PostgreSQL, or SQLite?
**Answer**:
1. **Enterprise Standard**: Oracle is the global standard for hospitals, pharmaceuticals, banking, and government healthcare infrastructure due to its battle-tested security and compliance features.
2. **Advanced PL/SQL**: Oracle's Compound Triggers and stored procedure engine allow complex business rules (like zero-tolerance overselling prevention) to be executed with microsecond latency directly adjacent to the storage blocks.
3. **High Availability**: Features like Oracle Real Application Clusters (RAC) and Data Guard enable 99.999% uptime for 24/7 hospital pharmacies.

---

### Q2: Can Al-Dawah Pharma run completely offline inside a clinic or local pharmacy?
**Answer**:
**Yes, 100% offline.**
The database runs locally (`localhost:1521`), the backend runs locally (`localhost:5091`), and the frontend is served directly by the backend web server as static files (`app.UseStaticFiles()`). No external cloud connection is required for day-to-day operations.

---

### Q3: How do daily database backups work?
**Answer**:
You can perform automated hot backups of the Oracle Database without stopping the pharmacy software using **Oracle Data Pump (`expdp`)**:
```bash
docker exec -it oracle expdp system/SecretPassword2026#@FREE \
  schemas=SYSTEM \
  directory=DATA_PUMP_DIR \
  dumpfile=aldawah_backup_%U.dmp \
  logfile=aldawah_backup.log
```
This dump file can be archived to external NAS drives or encrypted cloud storage daily.

---

### Q4: How do I deploy this to a Linux production server (Ubuntu/Debian/RHEL)?
**Answer**:
1. Run Oracle Database via Docker or Podman.
2. Publish the .NET application:
   ```bash
   dotnet publish src/Backend/Api -c Release -o /var/www/aldawah-pharma
   ```
3. Create a systemd service (`/etc/systemd/system/aldawah.service`) to keep the .NET process running 24/7.
4. Configure **Nginx** as a reverse proxy with a free SSL certificate from **Let's Encrypt** (HTTPS on port 443).
