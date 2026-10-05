# Al-Dawah Pharma - REST API Specification

## 1. Overview
The **Al-Dawah Pharma API** is built using **ASP.NET Core Web API**. It enforces standard HTTP status codes, structured JSON responses (`ApiResponse<T>`), JWT Bearer authentication, and role authorization.

### Standard Response Envelope
```json
{
  "success": true,
  "message": "Operation description",
  "data": { ... },
  "errors": null,
  "timestamp": "2026-10-05T05:35:49Z"
}
```

---

## 2. Authentication Endpoints

### `POST /api/auth/login`
- **Anonymous Access**
- **Request Body**:
  ```json
  {
    "username": "admin",
    "password": "admin123"
  }
  ```
- **Response `200 OK`**:
  ```json
  {
    "success": true,
    "data": {
      "token": "eyJhbGciOiJIUzI1Ni...",
      "userID": 1,
      "username": "admin",
      "fullName": "Admin User",
      "role": "Admin",
      "expiresAt": "2026-10-05T17:35:55Z"
    }
  }
  ```

### `GET /api/auth/me`
- **Requires `Authorization: Bearer <token>`**
- Returns the authenticated user's profile and assigned role.

### `POST /api/auth/change-password`
- **Requires `Authorization: Bearer <token>`**
- Updates the current user's password with BCrypt hashing.

---

## 3. Dashboard Endpoints

### `GET /api/dashboard/summary`
- **Authorized (Staff or Admin)**
- Returns aggregate metrics (Total Medicines, Total Categories, Total Companies, Total Suppliers, Total Customers, Low Stock Count, Expired Count, Near Expiry Count, Inventory Valuation, Total Sales Revenue, Total Purchases Expenditure, monthly chart data, and top selling drugs).

---

## 4. Medicine Endpoints

| Method | Endpoint | Description | Auth Policy |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/medicines` | Query catalog with optional search, category, or company filters | All Staff/Admin |
| `GET` | `/api/medicines/{id}` | Retrieve single medicine details | All Staff/Admin |
| `POST` | `/api/medicines` | Create medicine (calls Oracle procedure `ADD_MEDICINE`) | Admin / Staff |
| `PUT` | `/api/medicines/{id}` | Update existing medicine fields | Admin / Staff |
| `DELETE` | `/api/medicines/{id}` | Delete medicine (blocked if transactions exist) | Admin Only |
| `GET` | `/api/medicines/{id}/stock` | Evaluate live stock via Oracle function `GET_AVAILABLE_STOCK` | All Staff/Admin |
| `GET` | `/api/medicines/{id}/valuation` | Evaluate inventory value via Oracle function `GET_INVENTORY_VALUE` | All Staff/Admin |

---

## 5. Transaction Endpoints (Purchases & Sales)

### `POST /api/purchases`
- **Authorized (Staff or Admin)**
- Records wholesale purchase. For single items, executes `RECORD_PURCHASE`. For multi-items, executes an atomic database transaction. SubTotal, stock increment, and TotalAmount are managed automatically by triggers.
- **Request Body**:
  ```json
  {
    "supplierID": 1,
    "items": [
      {
        "medicineID": 1,
        "quantity": 50,
        "unitPrice": 6.50
      }
    ]
  }
  ```

### `POST /api/sales`
- **Authorized (Staff or Admin)**
- Records retail sale. For single items, executes `RECORD_SALE`. For multi-items, executes an atomic transaction. Stock sufficiency is checked by `TRG_SALE_STOCK`. If stock is insufficient, returns `400 Bad Request` (`ORA-20001`).
- **Request Body**:
  ```json
  {
    "customerID": 1,
    "items": [
      {
        "medicineID": 1,
        "quantity": 2,
        "unitPrice": 10.00
      }
    ]
  }
  ```

---

## 6. Analytical Reports Endpoints

All reports directly query the corresponding Oracle views and optimized aggregate SQL queries:

- `GET /api/reports/expired`: Reads from `ExpiredMedicine_View`.
- `GET /api/reports/near-expiry`: Reads from `NearExpiryMedicine_View`.
- `GET /api/reports/low-stock`: Reads from `LowStock_View`.
- `GET /api/reports/monthly-sales`: Reads from `MonthlySales_View`.
- `GET /api/reports/highest-selling?top=10`: Top selling medicines ranked by volume.
- `GET /api/reports/company-stock`: Reads from `CompanyWiseStock_View`.
- `GET /api/reports/supplier-purchase`: Reads from `SupplierWisePurchase_View`.
- `GET /api/reports/inventory-value`: Reads from `InventoryValue_View`.
