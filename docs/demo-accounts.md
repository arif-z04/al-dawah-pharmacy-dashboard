# Al-Dawah Pharma - Development & Demo Accounts

This document contains development and demo credentials for evaluating and testing the **Al-Dawah Pharma** Pharmacy Inventory & Sales Management System.

> **Production Security Notice:**
> - In accordance with strict production requirements, no "demo" fields (e.g., `IsDemo`, `DemoUser`, `DemoPassword`) exist in the database schema.
> - All passwords in the `USERS` table are securely hashed using **BCrypt** (work factor 11).
> - Plaintext passwords are never stored in the database.
> - The credentials below are intended strictly for local development, staging, and academic/demonstration evaluation.

---

## 1. Development Accounts

| Account Role | Username | Password | User ID | Permissions Summary |
| :--- | :--- | :--- | :--- | :--- |
| **System Admin** | `admin` | `admin123` | `1` | Full administrative access: CRUD on all tables, execute administrative procedures, view audit logs, manage user accounts & security, access all financial & inventory reports. |
| **Pharmacist / Staff** | `rahim` | `rahim123` | `2` | Operational access: Point-of-sale dispensing, purchase invoice entry, customer registry, stock log viewing, expiry alert review. Resticted from administrative user management. |
| **Pharmacist / Staff** | `karim` | `karim123` | `3` | Operational staff account for counter sales and inventory handling. |
| **Pharmacist / Staff** | `nusrat` | `nusrat123` | `4` | Operational staff account for counter sales and inventory handling. |

---

## 2. Testing Role-Based Access Control (RBAC)

1. **Sign in as Admin (`admin` / `admin123`)**:
   - The sidebar displays the **Administration -> Users & Roles** navigation section.
   - You can create new user accounts, modify roles, and change system settings.
   - You can add categories, add medicines, and delete records.

2. **Sign in as Staff (`rahim` / `rahim123`)**:
   - The **Administration -> Users & Roles** section is hidden.
   - If an unauthorized request is made directly to `/api/users`, the backend rejects it with `403 Forbidden` (`RequireAdmin` policy).
   - Staff can freely conduct sales, register purchases, look up medicines, inspect low-stock views, and view analytical reports.

---

## 3. Database Application Account

The application backend connects to the Oracle Database using a dedicated least-privilege service account rather than `SYS` or `SYSTEM`:

- **Username**: `C##PHARMACY_APP`
- **Password**: `PharmacyApp2026#`
- **Database Service**: `localhost:1521/FREE` (or `localhost:1521/XE`)
- **Roles Granted**: `C##PHARMACY_ADMIN`, `C##PHARMACY_STAFF`, `CONNECT`, `RESOURCE`
