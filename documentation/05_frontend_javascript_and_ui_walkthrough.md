# Chapter 5: Frontend Architecture & UI Walkthrough

In this chapter, we explore the user interface of **Al-Dawah Pharma**.

The frontend is built as a **Single Page Application (SPA)** using native, standard **HTML5**, **Tailwind CSS**, modern **JavaScript (ES6+)**, and **Lucide Icons**. It requires zero external Node.js build tooling, compiles in zero milliseconds, and runs natively in every modern web browser.

---

## 1. Frontend Architectural Philosophy

In contemporary web development, many developers reflexively install heavy JavaScript frameworks (React, Angular, Vue, Next.js) requiring thousands of `node_modules` dependencies, complex Webpack/Vite bundlers, and gigabytes of disk overhead.

For an operational pharmacy management system, we chose **Vanilla ES6+ JavaScript**:
1. **Zero Build Dependency Overhead**: You do not need `npm install`, Node.js runtimes, or fragile bundlers. You can double-click or serve the files immediately.
2. **Instant Performance**: No virtual DOM overhead. The browser executes native DOM operations at maximum hardware speed.
3. **Decoupled Architecture**: The frontend communicates with the backend exclusively via standard HTTP REST calls (`fetch()`). The frontend can be hosted anywhere (Nginx, Apache, IIS, Amazon S3, Cloudflare Pages) without changing a single line of backend C# code.
4. **Permanent Stability**: Native browser standards (HTML5, DOM APIs, CSS Grid, ES6 modules) do not undergo breaking framework deprecations every six months.

---

## 2. Directory Structure & File Roles

The frontend is organized under `src/Frontend/`:

```
src/Frontend/
├── index.html          # Application shell, layout skeleton, modal and toast containers
├── css/
│   └── styles.css      # Custom styling, animations, print stylesheet (@media print)
└── js/
    ├── api.js          # Centralized HTTP REST client with JWT token handling
    ├── auth.js         # Authentication state, login/logout, role-based UI gating
    └── app.js          # SPA router, module renderers, state management, event handlers
```

---

## 3. UI Shell Architecture: `index.html`

`index.html` is the single HTML page delivered to the user's browser. It provides:
1. **Sidebar Navigation**: Desktop and collapsible mobile drawer with icons and badges.
2. **Header Bar**: Live pharmacy brand, current view breadcrumbs, user avatar, and session logout.
3. **Dynamic Viewport (`#content-area`)**: The target `<div>` where JavaScript renders screens on the fly without refreshing the page.
4. **Overlay Containers**:
   - `#toast-container`: Top-right animated notifications for success and error alerts.
   - `#modal-container`: Dynamic pop-ups for medicine creation, purchase entry, and invoice printing.
   - `#confirm-modal`: Promise-based confirmation dialog.

```html
<!DOCTYPE html>
<html lang="en" class="h-full bg-slate-50">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Al-Dawah Pharma - Pharmacy Inventory & Sales System</title>
  <script src="https://cdn.tailwindcss.com"></script>
  <script src="https://unpkg.com/lucide@latest"></script>
  <link rel="stylesheet" href="css/styles.css">
</head>
<body class="h-full flex flex-col font-sans text-slate-800 antialiased select-none">
  <!-- Toast Container -->
  <div id="toast-container" class="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none"></div>

  <!-- Main App Shell -->
  <div id="app" class="flex h-screen overflow-hidden">
    <!-- Sidebar -->
    <aside id="sidebar" class="w-64 bg-slate-900 text-slate-300 flex flex-col flex-shrink-0 transition-all duration-300">
      <!-- Navigation items rendered dynamically based on user role -->
      <nav id="sidebar-nav" class="flex-1 px-3 py-4 space-y-1 overflow-y-auto"></nav>
    </aside>

    <!-- Main Content Area -->
    <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
      <header class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6">
        <h1 id="page-title" class="text-xl font-bold text-slate-800">Dashboard</h1>
        <div id="user-badge" class="flex items-center gap-3"></div>
      </header>
      <main id="content-area" class="flex-1 overflow-y-auto p-6 bg-slate-50"></main>
    </div>
  </div>

  <script src="js/auth.js"></script>
  <script src="js/api.js"></script>
  <script src="js/app.js"></script>
</body>
</html>
```

---

## 4. The API Communication Layer: `api.js`

`api.js` is the sole gateway for network communication between the user's browser and the backend server.

### Centralized `request()` Method
Every outgoing API call passes through an asynchronous `request()` pipeline:
- **Automatic JWT Attachment**: If a token exists in `localStorage`, it attaches `Authorization: Bearer <token>`.
- **Automatic Content-Type**: Ensures `Content-Type: application/json` is included on all mutation requests.
- **Session Expiry Interception (401)**: If the backend returns HTTP 401 Unauthorized, `api.js` automatically clears the expired session, fires a custom event `auth:expired`, and displays a login prompt.
- **Error Normalization**: Flattens backend validation error dictionaries into readable messages.

```javascript
// Excerpt from src/Frontend/js/api.js
const Api = (() => {
  const BASE_URL = window.location.origin + '/api';

  async function request(endpoint, options = {}) {
    const url = `${BASE_URL}${endpoint}`;
    const token = Auth.getToken();

    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      ...(options.headers || {})
    };

    const response = await fetch(url, { ...options, headers });

    if (response.status === 401) {
      Auth.clearSession();
      window.dispatchEvent(new CustomEvent('auth:expired'));
      throw new Error('Your session has expired. Please sign in again.');
    }

    const json = await response.json().catch(() => null);

    if (!response.ok) {
      let errorMsg = json?.message || `Request failed with status ${response.status}`;
      if (json?.errors) {
        const details = Object.entries(json.errors)
          .map(([f, msgs]) => `${f}: ${msgs.join(', ')}`).join('; ');
        if (details) errorMsg += ` (${details})`;
      }
      throw new Error(errorMsg);
    }

    return json?.data !== undefined ? json.data : json;
  }

  return {
    // Auth Endpoints
    login: (data) => request('/auth/login', { method: 'POST', body: JSON.stringify(data) }),
    getProfile: () => request('/auth/me'),

    // Medicines Endpoints
    getMedicines: (params = {}) => {
      const q = new URLSearchParams(params).toString();
      return request(`/medicines${q ? '?' + q : ''}`);
    },
    createMedicine: (data) => request('/medicines', { method: 'POST', body: JSON.stringify(data) }),
    updateMedicine: (id, data) => request(`/medicines/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Stock Movement Endpoints
    getStockLogs: (params = {}) => {
      const q = new URLSearchParams(params).toString();
      return request(`/stock-log${q ? '?' + q : ''}`);
    },
    recordStockAdjustment: (data) => request('/stock/adjust', { method: 'POST', body: JSON.stringify(data) }),

    // Sales Endpoints
    getSales: () => request('/sales'),
    createSale: (data) => request('/sales', { method: 'POST', body: JSON.stringify(data) })
  };
})();
```

---

## 5. Authentication & Role-Based UI Gating: `auth.js`

`auth.js` manages client-side user sessions and provides security guards for user interface controls:

```javascript
// Excerpt from src/Frontend/js/auth.js
const Auth = (() => {
  const TOKEN_KEY = 'aldawah_token';
  const USER_KEY = 'aldawah_user';

  return {
    getToken: () => localStorage.getItem(TOKEN_KEY),
    getUser: () => {
      const u = localStorage.getItem(USER_KEY);
      return u ? JSON.parse(u) : null;
    },
    isAdmin: () => {
      const user = Auth.getUser();
      return user && user.role === 'Admin';
    },
    isAuthenticated: () => !!localStorage.getItem(TOKEN_KEY),
    setSession: (token, user) => {
      localStorage.setItem(TOKEN_KEY, token);
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    },
    clearSession: () => {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    }
  };
})();
```

---

## 6. Application Controller & POS Engine: `app.js`

`app.js` is the central brain of the frontend application. It handles routing, global caches, toast notifications, confirmation dialogs, and the interactive Point of Sale (POS) checkout module.

### Toast Notification System
Provides non-blocking, self-dismissing animated alerts:
```javascript
function showToast(message, type = 'info') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  const bg = type === 'success' ? 'bg-emerald-600' : type === 'error' ? 'bg-red-600' : type === 'warning' ? 'bg-amber-600' : 'bg-slate-800';
  const icon = type === 'success' ? 'check-circle' : type === 'error' ? 'x-circle' : type === 'warning' ? 'alert-triangle' : 'info';

  toast.className = `${bg} text-white px-4 py-3 rounded-xl shadow-xl flex items-center gap-3 text-xs font-semibold fade-in pointer-events-auto transition-all duration-300`;
  toast.innerHTML = `
    <i data-lucide="${icon}" class="w-4 h-4 flex-shrink-0"></i>
    <span class="flex-1">${message}</span>
    <button class="text-white/80 hover:text-white" onclick="this.parentElement.remove()">
      <i data-lucide="x" class="w-3.5 h-3.5"></i>
    </button>
  `;
  container.appendChild(toast);
  lucide.createIcons();

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(-10px)';
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}
```

### Point of Sale (POS) Interactive Cart Mechanics
The POS interface allows cashiers to search drugs, adjust quantities, calculate discounts, and complete sales:
- **Stock Validation**: Prevents adding more units than currently in stock (`item.quantity < medicine.quantityInStock`).
- **Mathematical Integrity**:
  $$\text{Line Subtotal} = \text{Quantity} \times \text{UnitPrice}$$
  $$\text{Invoice Total} = \sum \text{Line Subtotals}$$
  $$\text{Grand Total} = \max(0, \text{Invoice Total} - \text{Discount})$$
  $$\text{Change Due} = \max(0, \text{Cash Tendered} - \text{Grand Total})$$
- **Atomic Submission**: Sends the complete order to `POST /api/sales`. If Oracle's trigger detects that another cashier bought the last unit, the red toast pops up: *"Cannot complete sale: Insufficient stock available."*
- **Receipt Modal**: On successful response, renders a printable invoice with patient name, cashier name, itemized table, total, and barcode.

---

In the next chapter, **[06: Security Best Practices & Production Readiness](file:///home/noir/Work/AL-Dawah_Pharma/documentation/06_security_best_practices_and_production_readiness.md)**, we examine how the application protects sensitive medical and financial data.
