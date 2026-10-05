/**
 * Al-Dawah Pharma - API Client Service
 * Enforces production-grade REST communication with ASP.NET Core API
 */

const Api = (() => {
  const getBaseUrl = () => {
    if (window.location.port === '5091' || window.location.port === '5000') {
      return '/api';
    }
    return localStorage.getItem('aldawah_api_url') || 'http://localhost:5091/api';
  };

  const getToken = () => localStorage.getItem('aldawah_token');

  async function request(endpoint, options = {}) {
    const url = `${getBaseUrl()}${endpoint.startsWith('/') ? endpoint : '/' + endpoint}`;
    const token = getToken();

    const headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...options.headers,
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });

      if (response.status === 401) {
        // Token expired or invalid
        localStorage.removeItem('aldawah_token');
        localStorage.removeItem('aldawah_user');
        window.dispatchEvent(new CustomEvent('auth:expired'));
        throw new Error('Your session has expired. Please sign in again.');
      }

      const json = await response.json().catch(() => null);

      if (!response.ok) {
        let errorMsg = json?.message || `Request failed with status ${response.status}`;
        if (json?.errors) {
          const errList = Object.entries(json.errors)
            .map(([field, msgs]) => `${field}: ${msgs.join(', ')}`)
            .join('; ');
          if (errList) errorMsg += ` (${errList})`;
        }
        throw new Error(errorMsg);
      }

      return json?.data !== undefined ? json.data : json;
    } catch (err) {
      console.error(`API Error [${endpoint}]:`, err);
      throw err;
    }
  }

  return {
    getBaseUrl,
    setBaseUrl: (url) => localStorage.setItem('aldawah_api_url', url),
    
    // Auth
    login: (credentials) => request('/auth/login', { method: 'POST', body: JSON.stringify(credentials) }),
    getMe: () => request('/auth/me'),
    changePassword: (data) => request('/auth/change-password', { method: 'POST', body: JSON.stringify(data) }),

    // Dashboard
    getDashboardSummary: () => request('/dashboard/summary'),

    // Medicines
    getMedicines: (params = {}) => {
      const q = new URLSearchParams();
      if (params.search) q.append('search', params.search);
      if (params.categoryId) q.append('categoryId', params.categoryId);
      if (params.companyId) q.append('companyId', params.companyId);
      const queryStr = q.toString() ? `?${q.toString()}` : '';
      return request(`/medicines${queryStr}`);
    },
    getMedicineById: (id) => request(`/medicines/${id}`),
    createMedicine: (data) => request('/medicines', { method: 'POST', body: JSON.stringify(data) }),
    updateMedicine: (id, data) => request(`/medicines/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    deleteMedicine: (id) => request(`/medicines/${id}`, { method: 'DELETE' }),
    getMedicineStock: (id) => request(`/medicines/${id}/stock`),
    getMedicineValuation: (id) => request(`/medicines/${id}/valuation`),

    // Categories
    getCategories: () => request('/categories'),
    getCategoryById: (id) => request(`/categories/${id}`),
    createCategory: (data) => request('/categories', { method: 'POST', body: JSON.stringify(data) }),

    // Companies
    getCompanies: () => request('/companies'),
    getCompanyById: (id) => request(`/companies/${id}`),
    createCompany: (data) => request('/companies', { method: 'POST', body: JSON.stringify(data) }),
    updateCompany: (id, data) => request(`/companies/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Suppliers
    getSuppliers: () => request('/suppliers'),
    getSupplierById: (id) => request(`/suppliers/${id}`),
    createSupplier: (data) => request('/suppliers', { method: 'POST', body: JSON.stringify(data) }),
    updateSupplier: (id, data) => request(`/suppliers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Customers
    getCustomers: () => request('/customers'),
    getCustomerById: (id) => request(`/customers/${id}`),
    createCustomer: (data) => request('/customers', { method: 'POST', body: JSON.stringify(data) }),
    updateCustomer: (id, data) => request(`/customers/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

    // Purchases
    getPurchases: () => request('/purchases'),
    getPurchaseById: (id) => request(`/purchases/${id}`),
    createPurchase: (data) => request('/purchases', { method: 'POST', body: JSON.stringify(data) }),

    // Sales
    getSales: () => request('/sales'),
    getSaleById: (id) => request(`/sales/${id}`),
    createSale: (data) => request('/sales', { method: 'POST', body: JSON.stringify(data) }),

    // Stock Management & Movement
    getStockLogs: (params = {}) => {
      const q = new URLSearchParams();
      if (params.actionType) q.append('actionType', params.actionType);
      if (params.medicineId) q.append('medicineId', params.medicineId);
      const queryStr = q.toString() ? `?${q.toString()}` : '';
      return request(`/stock-log${queryStr}`);
    },
    recordStockAdjustment: (data) => request('/stock/adjust', { method: 'POST', body: JSON.stringify(data) }),

    // Expiry Alerts
    getExpiryAlerts: (status) => {
      const q = status ? `?status=${encodeURIComponent(status)}` : '';
      return request(`/expiryalerts${q}`);
    },
    markAlertNotificationSent: (id) => request(`/expiryalerts/${id}/notification-sent`, { method: 'PUT' }),

    // Analytical Reports
    getExpiredReport: () => request('/reports/expired'),
    getNearExpiryReport: () => request('/reports/near-expiry'),
    getLowStockReport: () => request('/reports/low-stock'),
    getMonthlySalesReport: () => request('/reports/monthly-sales'),
    getTopSellingReport: (top = 10) => request(`/reports/highest-selling?top=${top}`),
    getCompanyStockReport: () => request('/reports/company-stock'),
    getSupplierPurchaseReport: () => request('/reports/supplier-purchase'),
    getInventoryValuationReport: () => request('/reports/inventory-value'),

    // Users (Admin Only)
    getUsers: () => request('/users'),
    getUserById: (id) => request(`/users/${id}`),
    createUser: (data) => request('/users', { method: 'POST', body: JSON.stringify(data) }),
    updateUser: (id, data) => request(`/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    changeUserPassword: (id, newPassword) => request(`/users/${id}/password`, {
      method: 'PUT',
      body: JSON.stringify({ newPassword })
    }),
  };
})();
