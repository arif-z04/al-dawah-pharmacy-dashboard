# Chapter 6: Security Best Practices & Production Readiness

A pharmacy management system handles sensitive financial transactions, regulated medical supplies, and patient records. A single vulnerability could allow bad actors to alter prices, divert controlled narcotics, or steal credentials.

This chapter details the defense-in-depth security architecture implemented across **Al-Dawah Pharma**.

---

## 1. Threat Modeling for Pharmacy Management Systems

In a retail or hospital pharmacy, threat modeling focuses on four primary attack vectors:

| Threat Category | Potential Attack Scenario | Al-Dawah Pharma Mitigation |
| :--- | :--- | :--- |
| **Inventory Tampering** | An employee steals medicines and modifies the database to hide missing stock. | Immutable `STOCKLOG` table populated by database triggers. Direct table updates blocked; least-privilege database user. |
| **Price / Subtotal Forgery** | A malicious cashier alters JavaScript variables to set medicine prices to $0.01. | The backend recalculates all subtotals independently from the database pricing table. Client-supplied subtotals are discarded. |
| **Overselling / Stock Drift** | Two cashiers sell the last unit of an antibiotic at the exact same millisecond. | `TRG_CHECK_STOCK_BEFORE_SALE` uses `SELECT ... FOR UPDATE` row-level locks, raising `ORA-20001` if stock is depleted. |
| **Credential Theft** | An attacker dumps the `USERS` database table. | Passwords are cryptographically salted and hashed using **BCrypt** (work factor 11). Plaintext is never stored. |

---

## 2. Authentication & Cryptographic Standards

### Why MD5 and SHA-256 Are Insufficient for Passwords
Modern consumer graphics cards (GPUs) can compute over 100 billion SHA-256 hashes every second. If an attacker acquires a database dump with SHA-256 hashes, they can crack 8-character passwords in a matter of minutes using pre-computed **Rainbow Tables**.

### BCrypt: Adaptive Key Derivation
Al-Dawah Pharma uses **BCrypt** with an adaptive work factor of 11:
1. **Cryptographic Salt**: Every password generates a unique 128-bit random salt. Even if two users share the password `"password123"`, their database hash strings look completely different.
2. **Computational Slowness by Design**: A work factor of 11 forces the CPU to perform $2^{11} = 2,048$ cryptographic rounds. This takes ~100 milliseconds for a legitimate login, but makes brute-force attacks computationally impossible.

```csharp
// Secure Password Hashing
public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

// Secure Password Verification
public bool Verify(string password, string passwordHash) =>
    BCrypt.Net.BCrypt.Verify(password, passwordHash);
```

---

## 3. JWT (JSON Web Token) Security Lifecycle

When a user signs in, the API generates a signed **JSON Web Token (JWT)**:

```
+-----------------------------------------------------------------------------------+
|                            ANATOMY OF A SECURE JWT                                |
+-----------------------------------------------------------------------------------+
|  eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9                                             |
|  .                                                                                |
|  eyJzdWIiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsInJvbGUiOiJBZG1pbiIsImV4cCI6MTgwMH0  |
|  .                                                                                |
|  dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk                                      |
+-----------------------------------------------------------------------------------+
|  [ HEADER (Algorithm) ] . [ PAYLOAD (Claims & Identity) ] . [ HMAC-SHA256 SIGNATURE ]
+-----------------------------------------------------------------------------------+
```

### Security Rules Implemented in Al-Dawah Pharma:
1. **Strong Secret Key**: The HMAC-SHA256 signing key is a high-entropy 256-bit string stored in environment variables or `appsettings.json`.
2. **Short-Lived Expiration**: Tokens expire automatically after 8 hours.
3. **Zero Clock Skew**: In `Program.cs`, `ClockSkew = TimeSpan.Zero` eliminates the default 5-minute grace period, ensuring tokens expire precisely at the designated timestamp.
4. **Stateless Signature Verification**: The server verifies the token signature on every request without needing to query the database, maximizing API throughput.

---

## 4. SQL Injection Prevention: Parameterized Queries

**SQL Injection (SQLi)** is the most dangerous vulnerability in database-backed applications. It occurs when untrusted user input is directly concatenated into a SQL query string:

### The Vulnerable Pattern (NEVER DO THIS):
```csharp
// VULNERABLE TO SQL INJECTION!
// If input is: "' OR '1'='1" -> The attacker bypasses authentication!
string query = "SELECT * FROM USERS WHERE Username = '" + username + "'";
```

### The Production Pattern (Implemented in Al-Dawah Pharma):
All database interactions use **parameterized queries** with Dapper and ADO.NET:
```csharp
var sql = "SELECT * FROM USERS WHERE Username = :Username";
var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
```
In a parameterized query, the database engine compiles the SQL command structure *before* inserting the user data. The database treats the user input strictly as a literal text value, making SQL injection mathematically impossible.

---

## 5. Defense-in-Depth: Multi-Layered RBAC

Role-Based Access Control is enforced at **three independent layers**:

```
+-----------------------------------------------------------------------+
|                       MULTI-LAYER DEFENSE IN DEPTH                    |
+-----------------------------------------------------------------------+
|                                                                       |
|  [ LAYER 1: FRONTEND UI ]                                             |
|  - Hides "User Management" sidebar links from Pharmacists             |
|  - Prevents accidental unauthorized clicks                            |
|                                                                       |
|  [ LAYER 2: ASP.NET CORE API ]                                        |
|  - [Authorize(Roles = "Admin")] attribute on controllers              |
|  - Returns HTTP 403 Forbidden if a Pharmacist calls /api/users        |
|                                                                       |
|  [ LAYER 3: ORACLE DATABASE ]                                         |
|  - App connects as C##PHARMACY_APP (Least Privilege)                  |
|  - Cannot execute DROP TABLE, ALTER SYSTEM, or access sys.tables      |
+-----------------------------------------------------------------------+
```

> [!WARNING]
> Frontend UI hiding is merely a visual convenience. An attacker can use Postman, curl, or browser developer tools to send direct HTTP requests. Security must **always** be enforced on the backend server via `[Authorize(Roles = "...")]`.

---

## 6. Concurrency Control & ACID Transactions

In high-volume pharmacies with multiple cashier terminals, two cashiers may attempt to sell the exact same medication simultaneously.

### The Race Condition Problem:
1. Terminal A checks stock: 1 unit available.
2. Terminal B checks stock: 1 unit available.
3. Terminal A sells 1 unit (Stock becomes 0).
4. Terminal B sells 1 unit (Stock becomes -1, or inventory is corrupted).

### How Al-Dawah Pharma Solves This:
In `TRG_CHECK_STOCK_BEFORE_SALE`, the query uses Oracle's row-level lock:
```sql
SELECT CurrentStock INTO v_stock
FROM MEDICINES
WHERE MedicineID = :NEW.MedicineID
FOR UPDATE; -- Puts an exclusive lock on this specific medicine row!
```
- When Terminal A executes this query, Oracle locks the row for Terminal A.
- Terminal B is forced to pause for a few milliseconds until Terminal A commits or aborts.
- When Terminal B evaluates stock, it sees `CurrentStock = 0`, and the trigger aborts Terminal B's sale with `ORA-20001: Insufficient stock`.
- The inventory count remains 100% accurate without data drift.

---

In the next chapter, **[07: Comprehensive Windows Setup Guide](file:///home/noir/Work/AL-Dawah_Pharma/documentation/07_setup_guide_windows.md)**, we provide a complete, foolproof installation walkthrough for Windows users.
