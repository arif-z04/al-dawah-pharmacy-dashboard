/**
 * Al-Dawah Pharma - Main Application Controller
 */

// Global State
let currentTab = 'dashboard';
let categoriesCache = [];
let companiesCache = [];
let suppliersCache = [];
let medicinesCache = [];
let customersCache = [];

// Currency formatter
const formatMoney = (amount) => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2
  }).format(amount || 0).replace('$', '৳ ');
};

const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  const d = new Date(dateStr);
  return isNaN(d.getTime()) ? dateStr : d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
};

// Toast Notifications
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

// Confirmation Modal
function confirmAction(title, message) {
  return new Promise((resolve) => {
    const modal = document.getElementById('confirm-modal');
    const titleEl = document.getElementById('confirm-title');
    const msgEl = document.getElementById('confirm-message');
    const cancelBtn = document.getElementById('confirm-cancel');
    const okBtn = document.getElementById('confirm-ok');

    titleEl.textContent = title;
    msgEl.textContent = message;
    modal.classList.remove('hidden');

    const cleanup = () => {
      modal.classList.add('hidden');
      cancelBtn.onclick = null;
      okBtn.onclick = null;
    };

    cancelBtn.onclick = () => { cleanup(); resolve(false); };
    okBtn.onclick = () => { cleanup(); resolve(true); };
  });
}

// Modal System
function openModal(htmlContent) {
  const modal = document.getElementById('app-modal');
  const content = document.getElementById('app-modal-content');
  content.innerHTML = htmlContent;
  modal.classList.remove('hidden');
  lucide.createIcons();
}

function closeModal() {
  const modal = document.getElementById('app-modal');
  modal.classList.add('hidden');
}

// Initialize Application
document.addEventListener('DOMContentLoaded', async () => {
  // Handle Authentication Events
  window.addEventListener('auth:expired', () => {
    showToast('Your session has expired. Please sign in again.', 'warning');
    renderAuthView();
  });

  window.addEventListener('auth:login', (e) => {
    showToast(`Welcome back, ${e.detail.fullName}!`, 'success');
    renderAppShell();
  });

  window.addEventListener('auth:logout', () => {
    showToast('Signed out successfully.', 'info');
    renderAuthView();
  });

  // Setup Event Listeners
  document.getElementById('form-login').addEventListener('submit', handleLogin);
  document.getElementById('btn-logout').addEventListener('click', () => Auth.logout());
  document.getElementById('btn-global-refresh').addEventListener('click', () => navigate(currentTab, true));

  // Sidebar navigation click delegation
  document.querySelectorAll('[data-nav]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const targetTab = btn.getAttribute('data-nav');
      navigate(targetTab);
    });
  });

  // Modal Backdrop Close
  document.getElementById('app-modal').addEventListener('click', (e) => {
    if (e.target.id === 'app-modal') closeModal();
  });

  // Check Existing Session
  if (Auth.isAuthenticated()) {
    renderAppShell();
  } else {
    renderAuthView();
  }
});

// View Navigation & Switching
async function navigate(tab, forceReload = false) {
  currentTab = tab;

  // Update navigation styling
  document.querySelectorAll('[data-nav]').forEach(btn => {
    const isTarget = btn.getAttribute('data-nav') === tab;
    btn.classList.toggle('bg-slate-800', isTarget);
    btn.classList.toggle('text-white', isTarget);
    btn.classList.toggle('text-slate-400', !isTarget);
  });

  // Update Top Bar Title
  const titles = {
    dashboard: 'Pharmacy Dashboard & Analytics',
    medicines: 'Medicine Catalog & Inventory',
    categories: 'Medicine Categories',
    companies: 'Pharmaceutical Companies',
    suppliers: 'Suppliers & Distributors',
    customers: 'Customer Directory',
    purchases: 'Purchase Invoices & Receiving',
    sales: 'Point of Sale & Invoices',
    'stock-log': 'Inventory Stock Movement Log',
    'expiry-alerts': 'Medicine Expiry Alerts',
    reports: 'Business & Inventory Reports',
    users: 'User Account & Security Management',
    settings: 'System & Account Settings'
  };

  document.getElementById('page-title').textContent = titles[tab] || 'Al-Dawah Pharma';

  const contentArea = document.getElementById('content-area');
  contentArea.innerHTML = `
    <div class="py-20 flex flex-col items-center justify-center text-slate-400">
      <i data-lucide="loader-2" class="w-8 h-8 animate-spin text-emerald-600 mb-3"></i>
      <p class="text-sm font-medium">Fetching real-time data from Oracle Database...</p>
    </div>
  `;
  lucide.createIcons();

  try {
    switch (tab) {
      case 'dashboard':
        await renderDashboard();
        break;
      case 'medicines':
        await renderMedicines();
        break;
      case 'categories':
        await renderCategories();
        break;
      case 'companies':
        await renderCompanies();
        break;
      case 'suppliers':
        await renderSuppliers();
        break;
      case 'customers':
        await renderCustomers();
        break;
      case 'purchases':
        await renderPurchases();
        break;
      case 'sales':
        await renderSales();
        break;
      case 'stock-log':
        await renderStockLog();
        break;
      case 'expiry-alerts':
        await renderExpiryAlerts();
        break;
      case 'reports':
        await renderReports();
        break;
      case 'users':
        if (!Auth.isAdmin()) {
          showToast('Access denied: Administrator role required.', 'error');
          navigate('dashboard');
          return;
        }
        await renderUsers();
        break;
      case 'settings':
        await renderSettings();
        break;
      default:
        contentArea.innerHTML = `<div class="p-8 text-center text-slate-500">Module not found.</div>`;
    }
  } catch (err) {
    contentArea.innerHTML = `
      <div class="p-8 text-center">
        <div class="w-12 h-12 rounded-full bg-red-100 text-red-600 flex items-center justify-center mx-auto mb-3">
          <i data-lucide="alert-triangle" class="w-6 h-6"></i>
        </div>
        <h3 class="text-base font-bold text-slate-800 mb-1">Failed to load ${titles[tab]}</h3>
        <p class="text-xs text-slate-500 max-w-md mx-auto mb-4">${err.message}</p>
        <button onclick="navigate('${tab}', true)" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-medium text-xs rounded-lg transition-colors">
          Retry
        </button>
      </div>
    `;
    lucide.createIcons();
  }
}

// Auth Rendering
function renderAuthView() {
  document.getElementById('view-app').classList.add('hidden');
  document.getElementById('view-login').classList.remove('hidden');
  lucide.createIcons();
}

function renderAppShell() {
  const user = Auth.getUser();
  document.getElementById('view-login').classList.add('hidden');
  document.getElementById('view-app').classList.remove('hidden');

  document.getElementById('user-display-name').textContent = user?.fullName || user?.username || 'User';
  document.getElementById('user-display-role').textContent = user?.role || 'Staff';
  document.getElementById('user-avatar-initial').textContent = (user?.fullName || user?.username || 'U')[0].toUpperCase();

  // Role based menu display
  const adminSection = document.getElementById('admin-nav-section');
  if (Auth.isAdmin()) {
    adminSection.classList.remove('hidden');
  } else {
    adminSection.classList.add('hidden');
  }

  lucide.createIcons();
  navigate('dashboard');
}

async function handleLogin(e) {
  e.preventDefault();
  const username = document.getElementById('login-username').value.trim();
  const password = document.getElementById('login-password').value;
  const errorEl = document.getElementById('login-error');
  const submitBtn = document.getElementById('btn-login-submit');

  errorEl.classList.add('hidden');
  submitBtn.disabled = true;
  submitBtn.innerHTML = `<i data-lucide="loader-2" class="w-4 h-4 animate-spin"></i><span>Signing in...</span>`;
  lucide.createIcons();

  try {
    await Auth.login(username, password);
  } catch (err) {
    errorEl.textContent = err.message || 'Login failed. Please check credentials.';
    errorEl.classList.remove('hidden');
    submitBtn.disabled = false;
    submitBtn.innerHTML = `<span>Sign In</span><i data-lucide="arrow-right" class="w-4 h-4"></i>`;
    lucide.createIcons();
  }
}

// =============================================================================
// MODULE 1: DASHBOARD
// =============================================================================
async function renderDashboard() {
  const data = await Api.getDashboardSummary();
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <!-- Top Stats Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
      
      <!-- Total Inventory Value -->
      <div class="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-start justify-between">
        <div>
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-500 mb-1">Inventory Valuation</p>
          <h3 class="text-2xl font-extrabold text-slate-800">${formatMoney(data.totalInventoryValue)}</h3>
          <p class="text-xs text-slate-500 mt-1 flex items-center gap-1">
            <span class="font-semibold text-emerald-600">${data.totalMedicines}</span> distinct medicines
          </p>
        </div>
        <div class="p-3 bg-emerald-50 text-emerald-600 rounded-xl">
          <i data-lucide="wallet" class="w-5 h-5"></i>
        </div>
      </div>

      <!-- Total Sales Revenue -->
      <div class="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-start justify-between">
        <div>
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-500 mb-1">Total Sales Revenue</p>
          <h3 class="text-2xl font-extrabold text-slate-800">${formatMoney(data.totalSalesRevenue)}</h3>
          <p class="text-xs text-slate-500 mt-1 flex items-center gap-1">
            <span class="font-semibold text-emerald-600">${data.totalSalesCount}</span> completed invoices
          </p>
        </div>
        <div class="p-3 bg-blue-50 text-blue-600 rounded-xl">
          <i data-lucide="receipt" class="w-5 h-5"></i>
        </div>
      </div>

      <!-- Low Stock Alert -->
      <div class="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-start justify-between">
        <div>
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-500 mb-1">Low Stock Medicines</p>
          <h3 class="text-2xl font-extrabold ${data.lowStockCount > 0 ? 'text-amber-600' : 'text-slate-800'}">${data.lowStockCount}</h3>
          <button onclick="navigate('reports')" class="text-xs text-amber-600 hover:underline mt-1 font-medium">
            View LowStock_View &rarr;
          </button>
        </div>
        <div class="p-3 bg-amber-50 text-amber-600 rounded-xl">
          <i data-lucide="alert-triangle" class="w-5 h-5"></i>
        </div>
      </div>

      <!-- Expiry Alerts -->
      <div class="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-start justify-between">
        <div>
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-500 mb-1">Expiry Attention</p>
          <div class="flex items-center gap-2">
            <span class="text-xl font-extrabold text-red-600">${data.expiredCount} Expired</span>
            <span class="text-slate-300">|</span>
            <span class="text-base font-bold text-amber-600">${data.nearExpiryCount} Near</span>
          </div>
          <button onclick="navigate('expiry-alerts')" class="text-xs text-red-600 hover:underline mt-1 font-medium">
            View Alerts &rarr;
          </button>
        </div>
        <div class="p-3 bg-red-50 text-red-600 rounded-xl">
          <i data-lucide="alert-octagon" class="w-5 h-5"></i>
        </div>
      </div>

    </div>

    <!-- Secondary Stats & Quick Actions -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
      <div class="bg-white p-4 rounded-xl border border-slate-200 flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-700">
          <i data-lucide="layers" class="w-5 h-5"></i>
        </div>
        <div>
          <p class="text-xs text-slate-500">Categories</p>
          <p class="text-base font-bold text-slate-800">${data.totalCategories}</p>
        </div>
      </div>
      <div class="bg-white p-4 rounded-xl border border-slate-200 flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-700">
          <i data-lucide="building-2" class="w-5 h-5"></i>
        </div>
        <div>
          <p class="text-xs text-slate-500">Companies</p>
          <p class="text-base font-bold text-slate-800">${data.totalCompanies}</p>
        </div>
      </div>
      <div class="bg-white p-4 rounded-xl border border-slate-200 flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-700">
          <i data-lucide="truck" class="w-5 h-5"></i>
        </div>
        <div>
          <p class="text-xs text-slate-500">Suppliers</p>
          <p class="text-base font-bold text-slate-800">${data.totalSuppliers}</p>
        </div>
      </div>
      <div class="bg-white p-4 rounded-xl border border-slate-200 flex items-center gap-3">
        <div class="w-10 h-10 rounded-lg bg-slate-100 flex items-center justify-center text-slate-700">
          <i data-lucide="users" class="w-5 h-5"></i>
        </div>
        <div>
          <p class="text-xs text-slate-500">Customers</p>
          <p class="text-base font-bold text-slate-800">${data.totalCustomers}</p>
        </div>
      </div>
    </div>

    <!-- Charts & Tables Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
      <!-- Sales Chart -->
      <div class="lg:col-span-2 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-base font-bold text-slate-800">Monthly Sales Revenue</h3>
            <p class="text-xs text-slate-500">Derived from MonthlySales_View aggregate view</p>
          </div>
          <button onclick="navigate('sales')" class="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1">
            <span>New Sale</span> &rarr;
          </button>
        </div>
        <div class="flex-1 min-h-[260px] relative">
          <canvas id="dashboardSalesChart"></canvas>
        </div>
      </div>

      <!-- Top Selling Medicines -->
      <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-base font-bold text-slate-800">Top Selling Drugs</h3>
            <p class="text-xs text-slate-500">By total units dispensed</p>
          </div>
        </div>
        <div class="flex-1 space-y-3 overflow-y-auto">
          ${(data.topSellingMedicines || []).map(m => `
            <div class="flex items-center justify-between p-3 rounded-xl bg-slate-50 border border-slate-100">
              <div class="truncate mr-2">
                <p class="text-xs font-bold text-slate-800 truncate">${m.medicineName}</p>
                <p class="text-[10px] text-slate-500">${m.categoryName} • ${m.companyName}</p>
              </div>
              <div class="text-right flex-shrink-0">
                <span class="px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 font-bold text-[11px]">${m.totalQuantitySold} sold</span>
                <p class="text-[10px] text-slate-500 mt-0.5">${formatMoney(m.totalRevenueGenerated)}</p>
              </div>
            </div>
          `).join('') || '<p class="text-xs text-slate-400 py-6 text-center">No sales records found.</p>'}
        </div>
      </div>
    </div>
  `;

  lucide.createIcons();

  // Render Chart
  if (data.recentSalesChart && data.recentSalesChart.length > 0) {
    const ctx = document.getElementById('dashboardSalesChart')?.getContext('2d');
    if (ctx) {
      const labels = data.recentSalesChart.map(s => s.salesMonth).reverse();
      const revenues = data.recentSalesChart.map(s => s.totalRevenue).reverse();

      new Chart(ctx, {
        type: 'bar',
        data: {
          labels,
          datasets: [{
            label: 'Monthly Revenue (৳)',
            data: revenues,
            backgroundColor: 'rgba(16, 185, 129, 0.85)',
            borderColor: 'rgb(5, 150, 105)',
            borderWidth: 1,
            borderRadius: 6
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false }
          },
          scales: {
            y: {
              beginAtZero: true,
              ticks: { font: { size: 10 } }
            },
            x: {
              ticks: { font: { size: 10 } }
            }
          }
        }
      });
    }
  }
}

// =============================================================================
// MODULE 2: MEDICINES
// =============================================================================
async function renderMedicines() {
  const [medicines, categories, companies, suppliers] = await Promise.all([
    Api.getMedicines(),
    Api.getCategories(),
    Api.getCompanies(),
    Api.getSuppliers()
  ]);

  medicinesCache = medicines;
  categoriesCache = categories;
  companiesCache = companies;
  suppliersCache = suppliers;

  const content = document.getElementById('content-area');

  content.innerHTML = `
    <!-- Top Action Bar -->
    <div class="bg-white p-4 rounded-2xl border border-slate-200 shadow-sm mb-6 flex flex-col md:flex-row items-stretch md:items-center justify-between gap-4">
      <div class="flex-1 flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
        <!-- Search Input -->
        <div class="relative flex-1">
          <span class="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400">
            <i data-lucide="search" class="w-4 h-4"></i>
          </span>
          <input type="text" id="med-search-input" placeholder="Search by name, generic, batch or barcode..."
            class="w-full pl-9 pr-4 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>

        <!-- Category Filter -->
        <select id="med-cat-filter" class="px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none bg-white">
          <option value="">All Categories (${categories.length})</option>
          ${categories.map(c => `<option value="${c.categoryID}">${c.categoryName}</option>`).join('')}
        </select>

        <!-- Company Filter -->
        <select id="med-comp-filter" class="px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none bg-white">
          <option value="">All Companies (${companies.length})</option>
          ${companies.map(cp => `<option value="${cp.companyID}">${cp.companyName}</option>`).join('')}
        </select>
      </div>

      <!-- Add Medicine Button (Invokes ADD_MEDICINE procedure) -->
      <button id="btn-add-medicine" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center justify-center gap-2 flex-shrink-0">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>Add Medicine (PL/SQL)</span>
      </button>
    </div>

    <!-- Medicines Table Card -->
    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">ID</th>
              <th class="py-3 px-4">Medicine & Generic</th>
              <th class="py-3 px-4">Batch / Form</th>
              <th class="py-3 px-4">Manufacturer</th>
              <th class="py-3 px-4 text-right">Pricing (Buy/Sell)</th>
              <th class="py-3 px-4 text-center">Stock</th>
              <th class="py-3 px-4">Expiry Date</th>
              <th class="py-3 px-4 text-center">Actions</th>
            </tr>
          </thead>
          <tbody id="medicines-table-body" class="divide-y divide-slate-100">
            ${renderMedicineRows(medicines)}
          </tbody>
        </table>
      </div>
      <div class="p-3 bg-slate-50 border-t border-slate-200 text-right text-xs text-slate-500 font-medium">
        Showing <span id="med-count" class="font-bold text-slate-800">${medicines.length}</span> medicines loaded from Oracle Database
      </div>
    </div>
  `;

  lucide.createIcons();

  // Attach search and filter listeners
  const filterHandler = () => {
    const term = document.getElementById('med-search-input').value.toLowerCase().trim();
    const catId = document.getElementById('med-cat-filter').value;
    const compId = document.getElementById('med-comp-filter').value;

    const filtered = medicinesCache.filter(m => {
      const matchTerm = !term || m.medicineName.toLowerCase().includes(term) ||
        (m.genericName && m.genericName.toLowerCase().includes(term)) ||
        m.batchNumber.toLowerCase().includes(term) ||
        (m.barcode && m.barcode.toLowerCase().includes(term));
      const matchCat = !catId || m.categoryID.toString() === catId;
      const matchComp = !compId || m.companyID.toString() === compId;
      return matchTerm && matchCat && matchComp;
    });

    document.getElementById('medicines-table-body').innerHTML = renderMedicineRows(filtered);
    document.getElementById('med-count').textContent = filtered.length;
    lucide.createIcons();
  };

  document.getElementById('med-search-input').addEventListener('input', filterHandler);
  document.getElementById('med-cat-filter').addEventListener('change', filterHandler);
  document.getElementById('med-comp-filter').addEventListener('change', filterHandler);
  document.getElementById('btn-add-medicine').addEventListener('click', () => openMedicineModal());
}

function renderMedicineRows(list) {
  if (!list || list.length === 0) {
    return `<tr><td colspan="8" class="py-12 text-center text-slate-400">No medicines found matching criteria.</td></tr>`;
  }

  const now = new Date();

  return list.map(m => {
    const isLow = m.quantityInStock <= m.reorderLevel;
    const expDate = new Date(m.expiryDate);
    const isExpired = expDate < now;
    const isNearExpiry = !isExpired && (expDate - now) / (1000 * 60 * 60 * 24) <= 30;

    return `
      <tr class="hover:bg-slate-50/80 transition-colors">
        <td class="py-3 px-4 font-mono font-bold text-slate-600">${m.medicineID}</td>
        <td class="py-3 px-4">
          <p class="font-bold text-slate-900">${m.medicineName}</p>
          <p class="text-[10px] text-slate-500 font-medium">${m.genericName || 'No generic name'}</p>
        </td>
        <td class="py-3 px-4">
          <span class="inline-block px-2 py-0.5 rounded font-mono font-medium text-[11px] bg-slate-100 text-slate-700">${m.batchNumber}</span>
          <span class="block text-[10px] text-slate-500 mt-0.5">${m.categoryName}</span>
        </td>
        <td class="py-3 px-4">
          <p class="font-medium text-slate-800">${m.companyName}</p>
          <p class="text-[10px] text-slate-500">${m.supplierName}</p>
        </td>
        <td class="py-3 px-4 text-right font-mono">
          <span class="text-slate-400 text-[10px] block">Buy: ${formatMoney(m.purchasePrice)}</span>
          <span class="text-slate-900 font-bold">${formatMoney(m.sellingPrice)}</span>
        </td>
        <td class="py-3 px-4 text-center">
          <span class="px-2.5 py-1 rounded-full text-xs font-bold ${isLow ? 'bg-red-100 text-red-700' : 'bg-emerald-100 text-emerald-800'}">
            ${m.quantityInStock}
          </span>
          ${isLow ? `<span class="block text-[9px] text-red-600 font-bold mt-0.5">Reorder (${m.reorderLevel})</span>` : ''}
        </td>
        <td class="py-3 px-4">
          <span class="${isExpired ? 'text-red-600 font-bold' : isNearExpiry ? 'text-amber-600 font-bold' : 'text-slate-700'}">
            ${formatDate(m.expiryDate)}
          </span>
          ${isExpired ? `<span class="inline-block px-1.5 py-0.2 rounded bg-red-100 text-red-700 text-[9px] font-bold ml-1">Expired</span>` : ''}
          ${isNearExpiry ? `<span class="inline-block px-1.5 py-0.2 rounded bg-amber-100 text-amber-700 text-[9px] font-bold ml-1">Near Expiry</span>` : ''}
        </td>
        <td class="py-3 px-4 text-center">
          <div class="inline-flex items-center gap-1.5">
            <button onclick="viewStockValuation(${m.medicineID})" title="Live Oracle Stock & Valuation" class="p-1.5 text-blue-600 hover:bg-blue-50 rounded">
              <i data-lucide="calculator" class="w-4 h-4"></i>
            </button>
            <button onclick="openMedicineModal(${m.medicineID})" title="Edit Medicine" class="p-1.5 text-slate-600 hover:bg-slate-100 rounded">
              <i data-lucide="edit-3" class="w-4 h-4"></i>
            </button>
            <button onclick="handleDeleteMedicine(${m.medicineID}, '${m.medicineName.replace(/'/g, "\\'")}')" title="Delete Medicine" class="p-1.5 text-red-600 hover:bg-red-50 rounded">
              <i data-lucide="trash-2" class="w-4 h-4"></i>
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join('');
}

// Medicine Modal (Add / Edit)
function openMedicineModal(medicineId = null) {
  const medicine = medicineId ? medicinesCache.find(m => m.medicineID === medicineId) : null;
  const isEdit = !!medicine;

  const html = `
    <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <i data-lucide="${isEdit ? 'edit' : 'plus-circle'}" class="w-5 h-5 text-emerald-600"></i>
        <h3 class="text-base font-bold text-slate-800">${isEdit ? 'Edit Medicine' : 'Add New Medicine'}</h3>
      </div>
      <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600">
        <i data-lucide="x" class="w-5 h-5"></i>
      </button>
    </div>

    <form id="medicine-form" class="p-6 overflow-y-auto space-y-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Brand Name *</label>
          <input type="text" id="m-name" required value="${medicine?.medicineName || ''}" placeholder="e.g. Napa 500"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Generic Name</label>
          <input type="text" id="m-generic" value="${medicine?.genericName || ''}" placeholder="e.g. Paracetamol"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Category *</label>
          <select id="m-category" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none bg-white">
            <option value="">Select Category</option>
            ${categoriesCache.map(c => `<option value="${c.categoryID}" ${medicine?.categoryID === c.categoryID ? 'selected' : ''}>${c.categoryName}</option>`).join('')}
          </select>
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Company *</label>
          <select id="m-company" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none bg-white">
            <option value="">Select Company</option>
            ${companiesCache.map(cp => `<option value="${cp.companyID}" ${medicine?.companyID === cp.companyID ? 'selected' : ''}>${cp.companyName}</option>`).join('')}
          </select>
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Supplier *</label>
          <select id="m-supplier" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none bg-white">
            <option value="">Select Supplier</option>
            ${suppliersCache.map(s => `<option value="${s.supplierID}" ${medicine?.supplierID === s.supplierID ? 'selected' : ''}>${s.supplierName}</option>`).join('')}
          </select>
        </div>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Batch Number *</label>
          <input type="text" id="m-batch" required value="${medicine?.batchNumber || ''}" placeholder="e.g. NP26001"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none font-mono">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Purchase Price (৳) *</label>
          <input type="number" step="0.01" min="0" id="m-purchase-price" required value="${medicine?.purchasePrice || ''}" placeholder="0.00"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Selling Price (৳) *</label>
          <input type="number" step="0.01" min="0" id="m-selling-price" required value="${medicine?.sellingPrice || ''}" placeholder="0.00"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Stock Quantity</label>
          <input type="number" min="0" id="m-stock" ${isEdit ? 'disabled' : ''} value="${medicine?.quantityInStock || 0}"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none ${isEdit ? 'bg-slate-100 cursor-not-allowed' : ''}">
          ${isEdit ? '<p class="text-[10px] text-slate-400 mt-0.5">Use Purchase/Sale/Log to adjust</p>' : ''}
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Reorder Level *</label>
          <input type="number" min="0" id="m-reorder" required value="${medicine?.reorderLevel ?? 10}"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Manufacturing Date *</label>
          <input type="date" id="m-mfg-date" required value="${medicine?.manufacturingDate ? medicine.manufacturingDate.substring(0, 10) : ''}"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Expiry Date *</label>
          <input type="date" id="m-exp-date" required value="${medicine?.expiryDate ? medicine.expiryDate.substring(0, 10) : ''}"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Barcode</label>
          <input type="text" id="m-barcode" value="${medicine?.barcode || ''}" placeholder="Scan or enter barcode"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none font-mono">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Description / Indications</label>
          <input type="text" id="m-desc" value="${medicine?.description || ''}" placeholder="Optional medicine notes"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
      </div>

      <div id="m-modal-error" class="hidden p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs"></div>

      <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
        <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
        <button type="submit" id="btn-save-medicine" class="px-5 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg flex items-center gap-1.5 shadow">
          <span>${isEdit ? 'Save Changes' : 'Execute ADD_MEDICINE'}</span>
        </button>
      </div>
    </form>
  `;

  openModal(html);

  document.getElementById('medicine-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errorEl = document.getElementById('m-modal-error');
    const saveBtn = document.getElementById('btn-save-medicine');
    errorEl.classList.add('hidden');

    const mfgDate = new Date(document.getElementById('m-mfg-date').value);
    const expDate = new Date(document.getElementById('m-exp-date').value);
    const pPrice = parseFloat(document.getElementById('m-purchase-price').value);
    const sPrice = parseFloat(document.getElementById('m-selling-price').value);

    if (expDate <= mfgDate) {
      errorEl.textContent = 'Validation error: Expiry date must be after manufacturing date.';
      errorEl.classList.remove('hidden');
      return;
    }

    if (sPrice < pPrice) {
      errorEl.textContent = 'Validation error: Selling price cannot be lower than purchase price.';
      errorEl.classList.remove('hidden');
      return;
    }

    const payload = {
      medicineName: document.getElementById('m-name').value.trim(),
      genericName: document.getElementById('m-generic').value.trim() || null,
      categoryID: parseInt(document.getElementById('m-category').value),
      companyID: parseInt(document.getElementById('m-company').value),
      supplierID: parseInt(document.getElementById('m-supplier').value),
      batchNumber: document.getElementById('m-batch').value.trim(),
      purchasePrice: pPrice,
      sellingPrice: sPrice,
      reorderLevel: parseInt(document.getElementById('m-reorder').value),
      manufacturingDate: document.getElementById('m-mfg-date').value,
      expiryDate: document.getElementById('m-exp-date').value,
      barcode: document.getElementById('m-barcode').value.trim() || null,
      description: document.getElementById('m-desc').value.trim() || null,
    };

    saveBtn.disabled = true;
    saveBtn.innerHTML = `<i data-lucide="loader-2" class="w-4 h-4 animate-spin"></i><span>Saving...</span>`;
    lucide.createIcons();

    try {
      if (isEdit) {
        await Api.updateMedicine(medicineId, payload);
        showToast(`Medicine '${payload.medicineName}' updated successfully.`, 'success');
      } else {
        payload.quantityInStock = parseInt(document.getElementById('m-stock').value) || 0;
        await Api.createMedicine(payload);
        showToast(`Medicine added via Oracle Stored Procedure ADD_MEDICINE!`, 'success');
      }
      closeModal();
      navigate('medicines', true);
    } catch (err) {
      errorEl.textContent = err.message || 'Failed to save medicine record.';
      errorEl.classList.remove('hidden');
      saveBtn.disabled = false;
      saveBtn.innerHTML = `<span>${isEdit ? 'Save Changes' : 'Execute ADD_MEDICINE'}</span>`;
      lucide.createIcons();
    }
  });
}

// Delete Medicine
async function handleDeleteMedicine(id, name) {
  const confirmed = await confirmAction('Delete Medicine', `Are you sure you want to permanently remove '${name}'? This will fail if transaction records exist.`);
  if (!confirmed) return;

  try {
    await Api.deleteMedicine(id);
    showToast(`Medicine '${name}' removed.`, 'success');
    navigate('medicines', true);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

// Live Oracle Functions Modal
async function viewStockValuation(medicineId) {
  const medicine = medicinesCache.find(m => m.medicineID === medicineId);
  if (!medicine) return;

  openModal(`
    <div class="p-8 text-center">
      <i data-lucide="loader-2" class="w-8 h-8 animate-spin text-emerald-600 mx-auto mb-3"></i>
      <p class="text-xs text-slate-500">Querying Oracle Functions GET_AVAILABLE_STOCK and GET_INVENTORY_VALUE...</p>
    </div>
  `);

  try {
    const [stock, valuation] = await Promise.all([
      Api.getMedicineStock(medicineId),
      Api.getMedicineValuation(medicineId)
    ]);

    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <i data-lucide="cpu" class="w-5 h-5 text-emerald-600"></i>
          <h3 class="text-base font-bold text-slate-800">Oracle PL/SQL Function Valuation</h3>
        </div>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600">
          <i data-lucide="x" class="w-5 h-5"></i>
        </button>
      </div>

      <div class="p-6 space-y-4">
        <div class="p-4 bg-slate-50 rounded-xl border border-slate-200">
          <h4 class="text-sm font-bold text-slate-800">${medicine.medicineName}</h4>
          <p class="text-xs text-slate-500">${medicine.genericName || ''} • Batch: ${medicine.batchNumber}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="p-4 rounded-xl border border-emerald-200 bg-emerald-50">
            <p class="text-[10px] font-bold uppercase text-emerald-700 tracking-wider">GET_AVAILABLE_STOCK</p>
            <p class="text-2xl font-extrabold text-emerald-900 mt-1">${stock} units</p>
            <p class="text-[10px] text-emerald-600 mt-1">Deterministic live inventory count</p>
          </div>

          <div class="p-4 rounded-xl border border-blue-200 bg-blue-50">
            <p class="text-[10px] font-bold uppercase text-blue-700 tracking-wider">GET_INVENTORY_VALUE</p>
            <p class="text-2xl font-extrabold text-blue-900 mt-1">${formatMoney(valuation)}</p>
            <p class="text-[10px] text-blue-600 mt-1">${stock} × ${formatMoney(medicine.purchasePrice)}</p>
          </div>
        </div>

        <div class="p-3 rounded-lg bg-slate-100 text-[11px] text-slate-600 font-mono">
          SELECT GET_AVAILABLE_STOCK(${medicineId}), GET_INVENTORY_VALUE(${medicineId}) FROM DUAL;
        </div>
      </div>

      <div class="p-4 border-t border-slate-200 flex justify-end">
        <button onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg">Close</button>
      </div>
    `);
  } catch (err) {
    showToast(err.message, 'error');
    closeModal();
  }
}

// =============================================================================
// MODULE 3: CATEGORIES
// =============================================================================
async function renderCategories() {
  const categories = await Api.getCategories();
  categoriesCache = categories;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Pharmaceutical Categories</h3>
        <p class="text-xs text-slate-500">Dosage forms and medicine classifications stored in Category table</p>
      </div>
      <button id="btn-add-category" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>Add Category (ADD_CATEGORY)</span>
      </button>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      ${categories.map(c => `
        <div class="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center font-mono font-bold text-sm">
              #${c.categoryID}
            </div>
            <div>
              <h4 class="text-sm font-bold text-slate-900">${c.categoryName}</h4>
              <p class="text-[11px] text-slate-500 font-medium">Oracle Entity</p>
            </div>
          </div>
          <span class="px-2.5 py-1 rounded-full text-[10px] font-bold bg-slate-100 text-slate-700">Active</span>
        </div>
      `).join('')}
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-add-category').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Add Category</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="category-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Category Name *</label>
          <input type="text" id="cat-name-input" required placeholder="e.g. Solution, Gel, Lozenge"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <p class="text-[11px] text-slate-500">Will be created via Oracle Stored Procedure ADD_CATEGORY with duplicate validation.</p>
        <div id="cat-modal-error" class="hidden p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs"></div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Save Category</button>
        </div>
      </form>
    `);

    document.getElementById('category-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      const name = document.getElementById('cat-name-input').value.trim();
      const errEl = document.getElementById('cat-modal-error');
      try {
        await Api.createCategory({ categoryName: name });
        showToast(`Category '${name}' created via ADD_CATEGORY.`, 'success');
        closeModal();
        navigate('categories', true);
      } catch (err) {
        errEl.textContent = err.message;
        errEl.classList.remove('hidden');
      }
    });
  });
}

// =============================================================================
// MODULE 4: COMPANIES
// =============================================================================
async function renderCompanies() {
  const companies = await Api.getCompanies();
  companiesCache = companies;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Pharmaceutical Manufacturers</h3>
        <p class="text-xs text-slate-500">${companies.length} partner pharmaceutical companies</p>
      </div>
      <button id="btn-add-company" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>Add Company</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">ID</th>
              <th class="py-3 px-4">Company Name</th>
              <th class="py-3 px-4">Address</th>
              <th class="py-3 px-4">Contact Phone</th>
              <th class="py-3 px-4">Email</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${companies.map(cp => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-600">${cp.companyID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${cp.companyName}</td>
                <td class="py-3 px-4 text-slate-600">${cp.address || 'N/A'}</td>
                <td class="py-3 px-4 font-mono text-slate-600">${cp.phone || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-600">${cp.email || 'N/A'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-add-company').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Add Company</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="company-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Company Name *</label>
          <input type="text" id="cp-name" required placeholder="e.g. Novartis Bangladesh"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Address</label>
          <input type="text" id="cp-address" placeholder="e.g. Tejgaon I/A, Dhaka"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Phone</label>
            <input type="text" id="cp-phone" placeholder="017xxxxxxxx"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Email</label>
            <input type="email" id="cp-email" placeholder="contact@company.com"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
        </div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Save Company</button>
        </div>
      </form>
    `);

    document.getElementById('company-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      try {
        await Api.createCompany({
          companyName: document.getElementById('cp-name').value.trim(),
          address: document.getElementById('cp-address').value.trim() || null,
          phone: document.getElementById('cp-phone').value.trim() || null,
          email: document.getElementById('cp-email').value.trim() || null
        });
        showToast('Company registered successfully.', 'success');
        closeModal();
        navigate('companies', true);
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
  });
}

// =============================================================================
// MODULE 5: SUPPLIERS
// =============================================================================
async function renderSuppliers() {
  const suppliers = await Api.getSuppliers();
  suppliersCache = suppliers;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Distributors & Suppliers</h3>
        <p class="text-xs text-slate-500">${suppliers.length} active wholesale medicine distributors</p>
      </div>
      <button id="btn-add-supplier" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>Add Supplier</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">ID</th>
              <th class="py-3 px-4">Supplier / Distributor</th>
              <th class="py-3 px-4">Contact Person</th>
              <th class="py-3 px-4">Phone</th>
              <th class="py-3 px-4">Email</th>
              <th class="py-3 px-4">Address</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${suppliers.map(s => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-600">${s.supplierID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${s.supplierName}</td>
                <td class="py-3 px-4 text-slate-700 font-medium">${s.contactPerson || 'N/A'}</td>
                <td class="py-3 px-4 font-mono text-slate-600">${s.phone || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-600">${s.email || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-500">${s.address || 'N/A'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-add-supplier').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Add Supplier</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="supplier-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Supplier Name *</label>
          <input type="text" id="sup-name" required placeholder="e.g. Apex Pharma Supply"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Contact Person</label>
          <input type="text" id="sup-contact" placeholder="e.g. Tariqul Islam"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Phone</label>
            <input type="text" id="sup-phone" placeholder="017xxxxxxxx"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Email</label>
            <input type="email" id="sup-email" placeholder="sales@supplier.com"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Address</label>
          <input type="text" id="sup-address" placeholder="e.g. Motijheel, Dhaka"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Save Supplier</button>
        </div>
      </form>
    `);

    document.getElementById('supplier-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      try {
        await Api.createSupplier({
          supplierName: document.getElementById('sup-name').value.trim(),
          contactPerson: document.getElementById('sup-contact').value.trim() || null,
          phone: document.getElementById('sup-phone').value.trim() || null,
          email: document.getElementById('sup-email').value.trim() || null,
          address: document.getElementById('sup-address').value.trim() || null
        });
        showToast('Supplier registered successfully.', 'success');
        closeModal();
        navigate('suppliers', true);
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
  });
}

// =============================================================================
// MODULE 6: CUSTOMERS
// =============================================================================
async function renderCustomers() {
  const customers = await Api.getCustomers();
  customersCache = customers;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Customer Directory</h3>
        <p class="text-xs text-slate-500">${customers.length} registered pharmacy retail clients</p>
      </div>
      <button id="btn-add-customer" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>Add Customer</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">ID</th>
              <th class="py-3 px-4">Customer Name</th>
              <th class="py-3 px-4">Phone Number</th>
              <th class="py-3 px-4">Address / City</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${customers.map(c => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-600">${c.customerID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${c.customerName}</td>
                <td class="py-3 px-4 font-mono text-slate-600">${c.phone || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-600">${c.address || 'N/A'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-add-customer').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Register Customer</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="customer-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Customer Full Name *</label>
          <input type="text" id="cust-name" required placeholder="e.g. Zahid Hasan"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Phone *</label>
            <input type="text" id="cust-phone" required placeholder="017xxxxxxxx"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Address / City</label>
            <input type="text" id="cust-address" placeholder="e.g. Uttara, Dhaka"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
        </div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Register Customer</button>
        </div>
      </form>
    `);

    document.getElementById('customer-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      try {
        await Api.createCustomer({
          customerName: document.getElementById('cust-name').value.trim(),
          phone: document.getElementById('cust-phone').value.trim(),
          address: document.getElementById('cust-address').value.trim() || null
        });
        showToast('Customer profile registered.', 'success');
        closeModal();
        navigate('customers', true);
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
  });
}

// =============================================================================
// MODULE 7: PURCHASES
// =============================================================================
async function renderPurchases() {
  const [purchases, suppliers, medicines] = await Promise.all([
    Api.getPurchases(),
    Api.getSuppliers(),
    Api.getMedicines()
  ]);

  suppliersCache = suppliers;
  medicinesCache = medicines;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Purchase Invoices</h3>
        <p class="text-xs text-slate-500">Inventory receipts from distributors (Triggers automatically increase stock and update totals)</p>
      </div>
      <button id="btn-new-purchase" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="plus-circle" class="w-4 h-4"></i>
        <span>New Purchase Order</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">Invoice #</th>
              <th class="py-3 px-4">Supplier / Distributor</th>
              <th class="py-3 px-4">Received Date</th>
              <th class="py-3 px-4">Processed By</th>
              <th class="py-3 px-4 text-right">Total Amount</th>
              <th class="py-3 px-4 text-center">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${purchases.map(p => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-700">#PUR-${p.purchaseID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${p.supplierName}</td>
                <td class="py-3 px-4 text-slate-600">${formatDate(p.purchaseDate)}</td>
                <td class="py-3 px-4 text-slate-600">${p.userName}</td>
                <td class="py-3 px-4 text-right font-mono font-bold text-slate-900">${formatMoney(p.totalAmount)}</td>
                <td class="py-3 px-4 text-center">
                  <button onclick="viewPurchaseDetails(${p.purchaseID})" class="px-2.5 py-1 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors">
                    View Details
                  </button>
                </td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-new-purchase').addEventListener('click', () => openNewPurchaseModal());
}

async function viewPurchaseDetails(purchaseId) {
  openModal(`<div class="p-8 text-center"><i data-lucide="loader-2" class="w-8 h-8 animate-spin text-emerald-600 mx-auto"></i></div>`);
  try {
    const purchase = await Api.getPurchaseById(purchaseId);
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <div>
          <h3 class="text-base font-bold text-slate-800">Purchase Order #PUR-${purchase.purchaseID}</h3>
          <p class="text-xs text-slate-500">${purchase.supplierName} • ${formatDate(purchase.purchaseDate)}</p>
        </div>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>

      <div class="p-6 space-y-4">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase">
            <tr>
              <th class="py-2.5 px-3">Medicine</th>
              <th class="py-2.5 px-3">Batch</th>
              <th class="py-2.5 px-3 text-center">Quantity</th>
              <th class="py-2.5 px-3 text-right">Unit Price</th>
              <th class="py-2.5 px-3 text-right">Subtotal</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 font-medium">
            ${purchase.details.map(d => `
              <tr>
                <td class="py-2.5 px-3 font-bold text-slate-800">${d.medicineName}</td>
                <td class="py-2.5 px-3 font-mono text-slate-500">${d.batchNumber}</td>
                <td class="py-2.5 px-3 text-center font-bold">${d.quantity}</td>
                <td class="py-2.5 px-3 text-right font-mono">${formatMoney(d.unitPrice)}</td>
                <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(d.subTotal)}</td>
              </tr>
            `).join('')}
          </tbody>
          <tfoot class="border-t-2 border-slate-300">
            <tr>
              <td colspan="4" class="py-3 px-3 text-right font-bold text-slate-700 uppercase">Grand Total:</td>
              <td class="py-3 px-3 text-right font-mono font-extrabold text-base text-emerald-700">${formatMoney(purchase.totalAmount)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div class="p-4 border-t border-slate-200 flex justify-end">
        <button onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg">Close</button>
      </div>
    `);
  } catch (err) {
    showToast(err.message, 'error');
    closeModal();
  }
}

function openNewPurchaseModal() {
  let items = [{ medicineId: medicinesCache[0]?.medicineID || 1, quantity: 10, unitPrice: medicinesCache[0]?.purchasePrice || 5.00 }];

  const renderItems = () => {
    const container = document.getElementById('purchase-items-container');
    if (!container) return;

    container.innerHTML = items.map((item, idx) => `
      <div class="grid grid-cols-12 gap-2 items-center p-2.5 bg-slate-50 rounded-xl border border-slate-200 text-xs">
        <div class="col-span-5">
          <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">Medicine</label>
          <select onchange="updatePurchaseItem(${idx}, 'medicineId', this.value)" class="w-full p-1.5 border border-slate-300 rounded text-xs bg-white">
            ${medicinesCache.map(m => `<option value="${m.medicineID}" ${item.medicineId == m.medicineID ? 'selected' : ''}>${m.medicineName} (${m.batchNumber})</option>`).join('')}
          </select>
        </div>
        <div class="col-span-3">
          <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">Qty</label>
          <input type="number" min="1" value="${item.quantity}" onchange="updatePurchaseItem(${idx}, 'quantity', this.value)"
            class="w-full p-1.5 border border-slate-300 rounded text-xs font-mono">
        </div>
        <div class="col-span-3">
          <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">Unit Price (৳)</label>
          <input type="number" step="0.01" min="0" value="${item.unitPrice}" onchange="updatePurchaseItem(${idx}, 'unitPrice', this.value)"
            class="w-full p-1.5 border border-slate-300 rounded text-xs font-mono">
        </div>
        <div class="col-span-1 pt-4 text-center">
          ${items.length > 1 ? `
            <button type="button" onclick="removePurchaseItem(${idx})" class="text-red-500 hover:text-red-700">
              <i data-lucide="trash-2" class="w-4 h-4"></i>
            </button>
          ` : ''}
        </div>
      </div>
    `).join('');

    // Calculate total
    const total = items.reduce((acc, it) => acc + (it.quantity * it.unitPrice), 0);
    const totalEl = document.getElementById('purchase-est-total');
    if (totalEl) totalEl.textContent = formatMoney(total);
    lucide.createIcons();
  };

  window.updatePurchaseItem = (idx, field, value) => {
    if (field === 'medicineId') {
      items[idx].medicineId = parseInt(value);
      const m = medicinesCache.find(med => med.medicineID == value);
      if (m) items[idx].unitPrice = m.purchasePrice;
    } else if (field === 'quantity') {
      items[idx].quantity = parseInt(value) || 1;
    } else if (field === 'unitPrice') {
      items[idx].unitPrice = parseFloat(value) || 0;
    }
    renderItems();
  };

  window.removePurchaseItem = (idx) => {
    items.splice(idx, 1);
    renderItems();
  };

  window.addPurchaseItem = () => {
    items.push({ medicineId: medicinesCache[0]?.medicineID || 1, quantity: 10, unitPrice: medicinesCache[0]?.purchasePrice || 5.00 });
    renderItems();
  };

  openModal(`
    <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
      <h3 class="text-base font-bold text-slate-800">New Purchase Order</h3>
      <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
    </div>

    <form id="new-purchase-form" class="p-6 space-y-4">
      <div>
        <label class="block text-xs font-semibold text-slate-700 mb-1">Select Supplier *</label>
        <select id="p-supplier-id" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs bg-white">
          ${suppliersCache.map(s => `<option value="${s.supplierID}">${s.supplierName} (${s.contactPerson || 'Distributor'})</option>`).join('')}
        </select>
      </div>

      <div>
        <div class="flex items-center justify-between mb-2">
          <label class="text-xs font-semibold text-slate-700">Order Items</label>
          <button type="button" onclick="addPurchaseItem()" class="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1">
            <i data-lucide="plus" class="w-3.5 h-3.5"></i> Add Item
          </button>
        </div>
        <div id="purchase-items-container" class="space-y-2 max-h-56 overflow-y-auto pr-1"></div>
      </div>

      <div class="p-3 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between">
        <span class="text-xs font-bold text-slate-700">Calculated Invoice Total:</span>
        <span id="purchase-est-total" class="text-sm font-extrabold text-emerald-700 font-mono">৳ 0.00</span>
      </div>

      <p class="text-[11px] text-slate-500">
        Single item purchase calls Oracle procedure <code>RECORD_PURCHASE</code>. Multi-item purchases use an atomic transaction; triggers compute SubTotal and update stock.
      </p>

      <div id="p-modal-error" class="hidden p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs"></div>

      <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
        <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
        <button type="submit" id="btn-submit-purchase" class="px-5 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg flex items-center gap-1.5 shadow">
          <span>Confirm & Record Purchase</span>
        </button>
      </div>
    </form>
  `);

  renderItems();

  document.getElementById('new-purchase-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errEl = document.getElementById('p-modal-error');
    const btn = document.getElementById('btn-submit-purchase');
    errEl.classList.add('hidden');

    const supplierId = parseInt(document.getElementById('p-supplier-id').value);
    const payload = {
      supplierID: supplierId,
      items: items.map(it => ({
        medicineID: it.medicineId,
        quantity: it.quantity,
        unitPrice: it.unitPrice
      }))
    };

    btn.disabled = true;
    btn.innerHTML = `<i data-lucide="loader-2" class="w-4 h-4 animate-spin"></i><span>Saving Purchase...</span>`;
    lucide.createIcons();

    try {
      await Api.createPurchase(payload);
      showToast('Purchase recorded successfully and stock updated!', 'success');
      closeModal();
      navigate('purchases', true);
    } catch (err) {
      errEl.textContent = err.message;
      errEl.classList.remove('hidden');
      btn.disabled = false;
      btn.innerHTML = `<span>Confirm & Record Purchase</span>`;
      lucide.createIcons();
    }
  });
}

// =============================================================================
// MODULE 8: SALES
// =============================================================================
async function renderSales() {
  const [sales, customers, medicines] = await Promise.all([
    Api.getSales(),
    Api.getCustomers(),
    Api.getMedicines()
  ]);

  customersCache = customers;
  medicinesCache = medicines;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Sales Invoices & POS</h3>
        <p class="text-xs text-slate-500">Retail sales (Oracle triggers validate stock sufficiency and decrement quantity)</p>
      </div>
      <button id="btn-new-sale" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="shopping-cart" class="w-4 h-4"></i>
        <span>New Sale Invoice</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">Invoice #</th>
              <th class="py-3 px-4">Customer</th>
              <th class="py-3 px-4">Date</th>
              <th class="py-3 px-4">Sold By</th>
              <th class="py-3 px-4 text-right">Total Amount</th>
              <th class="py-3 px-4 text-center">Receipt</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${sales.map(s => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-700">#INV-${s.saleID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${s.customerName}</td>
                <td class="py-3 px-4 text-slate-600">${formatDate(s.saleDate)}</td>
                <td class="py-3 px-4 text-slate-600">${s.userName}</td>
                <td class="py-3 px-4 text-right font-mono font-bold text-slate-900">${formatMoney(s.totalAmount)}</td>
                <td class="py-3 px-4 text-center">
                  <button onclick="viewSaleDetails(${s.saleID})" class="px-2.5 py-1 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors">
                    View Invoice
                  </button>
                </td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-new-sale').addEventListener('click', () => openNewSaleModal());
}

async function viewSaleDetails(saleId) {
  openModal(`<div class="p-8 text-center"><i data-lucide="loader-2" class="w-8 h-8 animate-spin text-emerald-600 mx-auto"></i></div>`);
  try {
    const sale = await Api.getSaleById(saleId);
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between bg-slate-900 text-white rounded-t-2xl">
        <div class="flex items-center gap-2">
          <i data-lucide="cross" class="w-5 h-5 text-emerald-400"></i>
          <div>
            <h3 class="text-sm font-bold tracking-wide">AL-DAWAH PHARMA INVOICE</h3>
            <p class="text-[10px] text-slate-400">#INV-${sale.saleID} • ${formatDate(sale.saleDate)}</p>
          </div>
        </div>
        <button onclick="closeModal()" class="text-slate-400 hover:text-white"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>

      <div class="p-6 space-y-4">
        <div class="flex justify-between items-center text-xs p-3 bg-slate-50 rounded-xl">
          <div>
            <span class="text-slate-400 block text-[10px] font-semibold uppercase">Customer</span>
            <span class="font-bold text-slate-800">${sale.customerName}</span>
          </div>
          <div class="text-right">
            <span class="text-slate-400 block text-[10px] font-semibold uppercase">Cashier</span>
            <span class="font-bold text-slate-800">${sale.userName}</span>
          </div>
        </div>

        <table class="w-full text-left text-xs">
          <thead class="bg-slate-100 border-b border-slate-200 text-slate-600 font-bold uppercase">
            <tr>
              <th class="py-2 px-3">Item Description</th>
              <th class="py-2 px-3">Batch</th>
              <th class="py-2 px-3 text-center">Qty</th>
              <th class="py-2 px-3 text-right">Price</th>
              <th class="py-2 px-3 text-right">Subtotal</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 font-medium">
            ${sale.details.map(d => `
              <tr>
                <td class="py-2 px-3 font-bold text-slate-800">${d.medicineName}</td>
                <td class="py-2 px-3 font-mono text-slate-500">${d.batchNumber}</td>
                <td class="py-2 px-3 text-center font-bold">${d.quantity}</td>
                <td class="py-2 px-3 text-right font-mono">${formatMoney(d.unitPrice)}</td>
                <td class="py-2 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(d.subTotal)}</td>
              </tr>
            `).join('')}
          </tbody>
          <tfoot class="border-t-2 border-slate-300">
            <tr>
              <td colspan="4" class="py-3 px-3 text-right font-bold text-slate-700 uppercase">Total Paid:</td>
              <td class="py-3 px-3 text-right font-mono font-extrabold text-base text-emerald-700">${formatMoney(sale.totalAmount)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div class="p-4 border-t border-slate-200 flex justify-between items-center">
        <span class="text-[11px] text-slate-400">Generated securely by Oracle Database Engine</span>
        <button onclick="window.print()" class="px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg flex items-center gap-1.5">
          <i data-lucide="printer" class="w-3.5 h-3.5"></i> Print Receipt
        </button>
      </div>
    `);
  } catch (err) {
    showToast(err.message, 'error');
    closeModal();
  }
}

function openNewSaleModal() {
  let items = [{ medicineId: medicinesCache[0]?.medicineID || 1, quantity: 1, unitPrice: medicinesCache[0]?.sellingPrice || 10.00 }];

  const renderItems = () => {
    const container = document.getElementById('sale-items-container');
    if (!container) return;

    container.innerHTML = items.map((item, idx) => {
      const selectedMed = medicinesCache.find(m => m.medicineID == item.medicineId);
      const availableStock = selectedMed?.quantityInStock || 0;
      const isOverStock = item.quantity > availableStock;

      return `
        <div class="grid grid-cols-12 gap-2 items-center p-2.5 bg-slate-50 rounded-xl border ${isOverStock ? 'border-red-300 bg-red-50/50' : 'border-slate-200'} text-xs">
          <div class="col-span-5">
            <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">
              Medicine (Stock: <span class="font-bold ${availableStock > 0 ? 'text-emerald-700' : 'text-red-600'}">${availableStock}</span>)
            </label>
            <select onchange="updateSaleItem(${idx}, 'medicineId', this.value)" class="w-full p-1.5 border border-slate-300 rounded text-xs bg-white">
              ${medicinesCache.map(m => `<option value="${m.medicineID}" ${item.medicineId == m.medicineID ? 'selected' : ''}>${m.medicineName} (${m.quantityInStock} in stock)</option>`).join('')}
            </select>
          </div>
          <div class="col-span-3">
            <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">Quantity</label>
            <input type="number" min="1" max="${availableStock}" value="${item.quantity}" onchange="updateSaleItem(${idx}, 'quantity', this.value)"
              class="w-full p-1.5 border ${isOverStock ? 'border-red-500 text-red-600' : 'border-slate-300'} rounded text-xs font-mono">
          </div>
          <div class="col-span-3">
            <label class="block text-[10px] text-slate-500 font-semibold mb-0.5">Unit Price (৳)</label>
            <input type="number" step="0.01" min="0" value="${item.unitPrice}" onchange="updateSaleItem(${idx}, 'unitPrice', this.value)"
              class="w-full p-1.5 border border-slate-300 rounded text-xs font-mono">
          </div>
          <div class="col-span-1 pt-4 text-center">
            ${items.length > 1 ? `
              <button type="button" onclick="removeSaleItem(${idx})" class="text-red-500 hover:text-red-700">
                <i data-lucide="trash-2" class="w-4 h-4"></i>
              </button>
            ` : ''}
          </div>
        </div>
      `;
    }).join('');

    const total = items.reduce((acc, it) => acc + (it.quantity * it.unitPrice), 0);
    const totalEl = document.getElementById('sale-est-total');
    if (totalEl) totalEl.textContent = formatMoney(total);
    lucide.createIcons();
  };

  window.updateSaleItem = (idx, field, value) => {
    if (field === 'medicineId') {
      items[idx].medicineId = parseInt(value);
      const m = medicinesCache.find(med => med.medicineID == value);
      if (m) items[idx].unitPrice = m.sellingPrice;
    } else if (field === 'quantity') {
      items[idx].quantity = parseInt(value) || 1;
    } else if (field === 'unitPrice') {
      items[idx].unitPrice = parseFloat(value) || 0;
    }
    renderItems();
  };

  window.removeSaleItem = (idx) => {
    items.splice(idx, 1);
    renderItems();
  };

  window.addSaleItem = () => {
    items.push({ medicineId: medicinesCache[0]?.medicineID || 1, quantity: 1, unitPrice: medicinesCache[0]?.sellingPrice || 10.00 });
    renderItems();
  };

  openModal(`
    <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
      <h3 class="text-base font-bold text-slate-800">Process Sale Invoice</h3>
      <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
    </div>

    <form id="new-sale-form" class="p-6 space-y-4">
      <div>
        <label class="block text-xs font-semibold text-slate-700 mb-1">Select Customer *</label>
        <select id="s-customer-id" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs bg-white">
          ${customersCache.map(c => `<option value="${c.customerID}">${c.customerName} (${c.phone || 'Retail'})</option>`).join('')}
        </select>
      </div>

      <div>
        <div class="flex items-center justify-between mb-2">
          <label class="text-xs font-semibold text-slate-700">Sale Line Items</label>
          <button type="button" onclick="addSaleItem()" class="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1">
            <i data-lucide="plus" class="w-3.5 h-3.5"></i> Add Line
          </button>
        </div>
        <div id="sale-items-container" class="space-y-2 max-h-56 overflow-y-auto pr-1"></div>
      </div>

      <div class="p-3 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between">
        <span class="text-xs font-bold text-slate-700">Total Invoice Amount:</span>
        <span id="sale-est-total" class="text-sm font-extrabold text-emerald-700 font-mono">৳ 0.00</span>
      </div>

      <p class="text-[11px] text-slate-500">
        Inventory check is enforced by Oracle Trigger <code>TRG_SALE_STOCK</code> (ORA-20001). If available stock is insufficient, transaction is automatically rolled back.
      </p>

      <div id="s-modal-error" class="hidden p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs"></div>

      <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
        <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
        <button type="submit" id="btn-submit-sale" class="px-5 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg flex items-center gap-1.5 shadow">
          <span>Complete Sale</span>
        </button>
      </div>
    </form>
  `);

  renderItems();

  document.getElementById('new-sale-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const errEl = document.getElementById('s-modal-error');
    const btn = document.getElementById('btn-submit-sale');
    errEl.classList.add('hidden');

    const customerId = parseInt(document.getElementById('s-customer-id').value);
    const payload = {
      customerID: customerId,
      items: items.map(it => ({
        medicineID: it.medicineId,
        quantity: it.quantity,
        unitPrice: it.unitPrice
      }))
    };

    btn.disabled = true;
    btn.innerHTML = `<i data-lucide="loader-2" class="w-4 h-4 animate-spin"></i><span>Processing Sale...</span>`;
    lucide.createIcons();

    try {
      await Api.createSale(payload);
      showToast('Sale invoice completed and inventory decremented!', 'success');
      closeModal();
      navigate('sales', true);
    } catch (err) {
      errEl.textContent = err.message;
      errEl.classList.remove('hidden');
      btn.disabled = false;
      btn.innerHTML = `<span>Complete Sale</span>`;
      lucide.createIcons();
    }
  });
}

// =============================================================================
// MODULE 9: STOCK LOG
// =============================================================================
async function renderStockLog() {
  const [logs, medicines] = await Promise.all([
    Api.getStockLogs(),
    Api.getMedicines()
  ]);

  medicinesCache = medicines;
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Inventory Movement Audit Trail</h3>
        <p class="text-xs text-slate-500">Every Purchase, Sale, and Adjustment recorded in StockLog table</p>
      </div>
      <button id="btn-record-adj" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="sliders" class="w-4 h-4"></i>
        <span>Record Stock Adjustment</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">Log ID</th>
              <th class="py-3 px-4">Medicine</th>
              <th class="py-3 px-4">Action Type</th>
              <th class="py-3 px-4 text-center">Quantity Delta</th>
              <th class="py-3 px-4">Responsible User</th>
              <th class="py-3 px-4">Timestamp</th>
              <th class="py-3 px-4">Remarks</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${logs.map(l => {
              const isPurchase = l.actionType === 'Purchase';
              const isSale = l.actionType === 'Sale';
              const badgeClass = isPurchase ? 'bg-emerald-100 text-emerald-800' : isSale ? 'bg-blue-100 text-blue-800' : 'bg-purple-100 text-purple-800';

              return `
                <tr class="hover:bg-slate-50 transition-colors">
                  <td class="py-3 px-4 font-mono font-bold text-slate-600">#LOG-${l.logID}</td>
                  <td class="py-3 px-4 font-bold text-slate-900">${l.medicineName}</td>
                  <td class="py-3 px-4">
                    <span class="px-2 py-0.5 rounded-full text-[10px] font-bold ${badgeClass}">${l.actionType}</span>
                  </td>
                  <td class="py-3 px-4 text-center font-mono font-bold ${isPurchase ? 'text-emerald-700' : isSale ? 'text-red-600' : 'text-purple-700'}">
                    ${isPurchase ? '+' : isSale ? '-' : '±'}${l.quantity}
                  </td>
                  <td class="py-3 px-4 text-slate-700">${l.userName}</td>
                  <td class="py-3 px-4 text-slate-500">${formatDate(l.actionDate)}</td>
                  <td class="py-3 px-4 text-slate-600 italic">${l.remarks || 'No remarks recorded'}</td>
                </tr>
              `;
            }).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-record-adj').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Record Stock Adjustment</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="adj-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Select Medicine *</label>
          <select id="adj-medicine" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs bg-white">
            ${medicinesCache.map(m => `<option value="${m.medicineID}">${m.medicineName} (Current: ${m.quantityInStock})</option>`).join('')}
          </select>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Adjustment Quantity Delta *</label>
            <input type="number" id="adj-quantity" required placeholder="e.g. +10 or -5"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none font-mono">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Movement Type</label>
            <input type="text" disabled value="Adjustment" class="w-full px-3 py-2 border border-slate-200 bg-slate-100 rounded-lg text-xs text-slate-500 font-semibold">
          </div>
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Audit Remarks *</label>
          <input type="text" id="adj-remarks" required placeholder="e.g. Physical inventory audit correction"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Record Adjustment</button>
        </div>
      </form>
    `);

    document.getElementById('adj-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      try {
        await Api.recordStockAdjustment({
          medicineID: parseInt(document.getElementById('adj-medicine').value),
          quantity: parseInt(document.getElementById('adj-quantity').value),
          remarks: document.getElementById('adj-remarks').value.trim(),
          reason: document.getElementById('adj-remarks').value.trim()
        });
        showToast('Stock adjustment logged successfully.', 'success');
        closeModal();
        navigate('stock-log', true);
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
  });
}

// =============================================================================
// MODULE 10: EXPIRY ALERTS
// =============================================================================
async function renderExpiryAlerts() {
  const alerts = await Api.getExpiryAlerts();
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">Medicine Expiry Alert Notifications</h3>
        <p class="text-xs text-slate-500">Real-time alerts tracking Expired and Near-Expiry catalog items from ExpiryAlert table</p>
      </div>
      <div class="flex items-center gap-2">
        <span class="px-2.5 py-1 rounded-lg bg-red-100 text-red-800 text-xs font-bold">
          ${alerts.filter(a => a.alertStatus === 'Expired').length} Expired
        </span>
        <span class="px-2.5 py-1 rounded-lg bg-amber-100 text-amber-800 text-xs font-bold">
          ${alerts.filter(a => a.alertStatus === 'Near Expiry').length} Near Expiry
        </span>
      </div>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">Alert #</th>
              <th class="py-3 px-4">Medicine Name</th>
              <th class="py-3 px-4">Batch Number</th>
              <th class="py-3 px-4">Alert Status</th>
              <th class="py-3 px-4">Alert Generated</th>
              <th class="py-3 px-4 text-center">Notification Status</th>
              <th class="py-3 px-4 text-center">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${alerts.map(a => {
              const isExpired = a.alertStatus === 'Expired';
              const isSent = a.notificationSent === 'Y';

              return `
                <tr class="hover:bg-slate-50 transition-colors">
                  <td class="py-3 px-4 font-mono font-bold text-slate-600">#ALT-${a.alertID}</td>
                  <td class="py-3 px-4 font-bold text-slate-900">${a.medicineName}</td>
                  <td class="py-3 px-4 font-mono text-slate-600">${a.batchNumber}</td>
                  <td class="py-3 px-4">
                    <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold ${isExpired ? 'bg-red-100 text-red-800' : 'bg-amber-100 text-amber-800'}">
                      ${a.alertStatus}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-slate-500">${formatDate(a.alertDate)}</td>
                  <td class="py-3 px-4 text-center">
                    <span class="px-2 py-0.5 rounded font-mono text-[10px] font-bold ${isSent ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-600'}">
                      ${isSent ? 'Notified (Y)' : 'Pending (N)'}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-center">
                    ${!isSent ? `
                      <button onclick="markNotification(${a.alertID})" class="px-2.5 py-1 text-xs font-semibold text-emerald-700 bg-emerald-50 hover:bg-emerald-100 rounded-lg transition-colors">
                        Mark Acknowledged
                      </button>
                    ` : '<span class="text-slate-400 text-xs italic">Acknowledged</span>'}
                  </td>
                </tr>
              `;
            }).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();
}

async function markNotification(alertId) {
  try {
    await Api.markAlertNotificationSent(alertId);
    showToast(`Expiry alert #ALT-${alertId} marked as notified.`, 'success');
    navigate('expiry-alerts', true);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

// =============================================================================
// MODULE 11: REPORTS (7 VIEWS + TOP SELLING)
// =============================================================================
async function renderReports() {
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <!-- Reports Navigation Bar -->
    <div class="bg-white p-2.5 rounded-2xl border border-slate-200 shadow-sm mb-6 flex flex-wrap gap-2">
      <button onclick="switchReportTab('expired')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-emerald-600 text-white" data-reptab="expired">
        1. Expired Medicines
      </button>
      <button onclick="switchReportTab('near-expiry')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="near-expiry">
        2. Near Expiry (30 Days)
      </button>
      <button onclick="switchReportTab('low-stock')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="low-stock">
        3. Low Stock Medicines
      </button>
      <button onclick="switchReportTab('monthly-sales')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="monthly-sales">
        4. Monthly Sales Aggregate
      </button>
      <button onclick="switchReportTab('top-selling')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="top-selling">
        5. Top Selling Medicines
      </button>
      <button onclick="switchReportTab('company-stock')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="company-stock">
        6. Company-Wise Stock
      </button>
      <button onclick="switchReportTab('supplier-purchase')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="supplier-purchase">
        7. Supplier Purchases
      </button>
      <button onclick="switchReportTab('inventory-value')" class="rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 text-slate-600 hover:bg-slate-200" data-reptab="inventory-value">
        8. Inventory Valuation
      </button>
    </div>

    <!-- Report Data Container -->
    <div id="report-view-container" class="bg-white rounded-2xl border border-slate-200 shadow-sm p-6 overflow-hidden">
      <div class="py-12 flex flex-col items-center justify-center text-slate-400">
        <i data-lucide="loader-2" class="w-6 h-6 animate-spin text-emerald-600 mb-2"></i>
        <p class="text-xs">Executing analytical query on Oracle Database...</p>
      </div>
    </div>
  `;

  lucide.createIcons();
  switchReportTab('expired');
}

async function switchReportTab(reportKey) {
  // Update Tab Button Styles
  document.querySelectorAll('.rep-tab').forEach(b => {
    const isTarget = b.getAttribute('data-reptab') === reportKey;
    b.className = `rep-tab px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors ${isTarget ? 'bg-emerald-600 text-white shadow' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`;
  });

  const container = document.getElementById('report-view-container');
  container.innerHTML = `
    <div class="py-12 flex flex-col items-center justify-center text-slate-400">
      <i data-lucide="loader-2" class="w-6 h-6 animate-spin text-emerald-600 mb-2"></i>
      <p class="text-xs font-medium">Querying Oracle View / Routine for ${reportKey}...</p>
    </div>
  `;
  lucide.createIcons();

  try {
    switch (reportKey) {
      case 'expired': {
        const data = await Api.getExpiredReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Expired Medicines Report</h4>
              <p class="text-[11px] text-slate-500">Source: <code>ExpiredMedicine_View</code> (WHERE ExpiryDate &lt; SYSDATE)</p>
            </div>
            <span class="px-2 py-0.5 rounded bg-red-100 text-red-700 text-xs font-bold">${data.length} Expired Batches</span>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Medicine</th>
                  <th class="py-2.5 px-3">Category</th>
                  <th class="py-2.5 px-3">Company</th>
                  <th class="py-2.5 px-3">Batch</th>
                  <th class="py-2.5 px-3 text-center">Remaining Stock</th>
                  <th class="py-2.5 px-3 text-right">Lost Capital (At Cost)</th>
                  <th class="py-2.5 px-3">Expired Date</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.medicineName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.categoryName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.companyName}</td>
                    <td class="py-2.5 px-3 font-mono font-bold text-slate-700">${r.batchNumber}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-red-600">${r.quantityInStock}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-red-700">${formatMoney(r.quantityInStock * r.purchasePrice)}</td>
                    <td class="py-2.5 px-3 text-red-600 font-bold">${formatDate(r.expiryDate)} (${r.daysExpired} days ago)</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'near-expiry': {
        const data = await Api.getNearExpiryReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Near-Expiry Medicines Report</h4>
              <p class="text-[11px] text-slate-500">Source: <code>NearExpiryMedicine_View</code> (WHERE ExpiryDate &gt;= SYSDATE AND &lt;= SYSDATE + 30)</p>
            </div>
            <span class="px-2 py-0.5 rounded bg-amber-100 text-amber-800 text-xs font-bold">${data.length} Batches Approaching Expiry</span>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Medicine</th>
                  <th class="py-2.5 px-3">Category</th>
                  <th class="py-2.5 px-3">Company</th>
                  <th class="py-2.5 px-3">Batch</th>
                  <th class="py-2.5 px-3 text-center">Current Stock</th>
                  <th class="py-2.5 px-3 text-right">Selling Price</th>
                  <th class="py-2.5 px-3">Expiry Date</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.medicineName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.categoryName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.companyName}</td>
                    <td class="py-2.5 px-3 font-mono font-bold text-slate-700">${r.batchNumber}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-amber-700">${r.quantityInStock}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(r.sellingPrice)}</td>
                    <td class="py-2.5 px-3 text-amber-600 font-bold">${formatDate(r.expiryDate)} (${r.daysRemaining} days left)</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'low-stock': {
        const data = await Api.getLowStockReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Low Stock Inventory Report</h4>
              <p class="text-[11px] text-slate-500">Source: <code>LowStock_View</code> (WHERE QuantityInStock &lt;= ReorderLevel)</p>
            </div>
            <span class="px-2 py-0.5 rounded bg-red-100 text-red-700 text-xs font-bold">${data.length} Below Reorder Level</span>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Medicine</th>
                  <th class="py-2.5 px-3">Supplier</th>
                  <th class="py-2.5 px-3 text-center">In Stock</th>
                  <th class="py-2.5 px-3 text-center">Reorder Threshold</th>
                  <th class="py-2.5 px-3 text-center">Shortfall Deficit</th>
                  <th class="py-2.5 px-3 text-right">Retail Price</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.medicineName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.supplierName}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-red-600">${r.quantityInStock}</td>
                    <td class="py-2.5 px-3 text-center font-mono font-medium">${r.reorderLevel}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-amber-700">-${r.stockDeficit} units</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold">${formatMoney(r.sellingPrice)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'monthly-sales': {
        const data = await Api.getMonthlySalesReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Monthly Sales Aggregate Report</h4>
              <p class="text-[11px] text-slate-500">Source: <code>MonthlySales_View</code> aggregate view</p>
            </div>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Sales Month</th>
                  <th class="py-2.5 px-3 text-center">Total Invoices</th>
                  <th class="py-2.5 px-3 text-center">Units Dispensed</th>
                  <th class="py-2.5 px-3 text-right">Gross Sales Revenue</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900 font-mono">${r.salesMonth}</td>
                    <td class="py-2.5 px-3 text-center font-semibold text-slate-700">${r.totalInvoices}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-emerald-700">${r.totalUnitsSold}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-extrabold text-slate-900">${formatMoney(r.totalRevenue)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'top-selling': {
        const data = await Api.getTopSellingReport(15);
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Highest Selling Medicines Report</h4>
              <p class="text-[11px] text-slate-500">Source: Optimized GROUP BY SalesDetails ranked by volume</p>
            </div>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Rank</th>
                  <th class="py-2.5 px-3">Medicine</th>
                  <th class="py-2.5 px-3">Manufacturer</th>
                  <th class="py-2.5 px-3 text-center">Total Units Sold</th>
                  <th class="py-2.5 px-3 text-right">Total Revenue</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map((r, i) => `
                  <tr>
                    <td class="py-2.5 px-3 font-mono font-bold text-slate-500">#${i + 1}</td>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.medicineName}</td>
                    <td class="py-2.5 px-3 text-slate-600">${r.companyName}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-emerald-700">${r.totalQuantitySold}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(r.totalRevenueGenerated)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'company-stock': {
        const data = await Api.getCompanyStockReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Company-Wise Stock & Valuation</h4>
              <p class="text-[11px] text-slate-500">Source: <code>CompanyWiseStock_View</code></p>
            </div>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Company</th>
                  <th class="py-2.5 px-3 text-center">Catalog SKUs</th>
                  <th class="py-2.5 px-3 text-center">Total Stock Units</th>
                  <th class="py-2.5 px-3 text-right">Inventory Valuation (Cost)</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.companyName}</td>
                    <td class="py-2.5 px-3 text-center font-semibold text-slate-700">${r.totalMedicineItems}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-emerald-700">${r.totalStockUnits}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(r.totalInventoryValue)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'supplier-purchase': {
        const data = await Api.getSupplierPurchaseReport();
        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Supplier-Wise Purchase Report</h4>
              <p class="text-[11px] text-slate-500">Source: <code>SupplierWisePurchase_View</code></p>
            </div>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Supplier Name</th>
                  <th class="py-2.5 px-3 text-center">Purchase Orders</th>
                  <th class="py-2.5 px-3 text-center">Units Supplied</th>
                  <th class="py-2.5 px-3 text-right">Total Expenditure</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.supplierName}</td>
                    <td class="py-2.5 px-3 text-center font-semibold text-slate-700">${r.totalPurchaseOrders}</td>
                    <td class="py-2.5 px-3 text-center font-bold text-emerald-700">${r.totalQuantityPurchased}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(r.totalPurchaseExpenditure)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
      case 'inventory-value': {
        const data = await Api.getInventoryValuationReport();
        const totalCost = data.reduce((acc, r) => acc + r.inventoryValuationAtCost, 0);
        const totalRetail = data.reduce((acc, r) => acc + r.projectedRevenueAtRetail, 0);
        const totalMargin = data.reduce((acc, r) => acc + r.projectedGrossMargin, 0);

        container.innerHTML = `
          <div class="flex items-center justify-between mb-4">
            <div>
              <h4 class="text-sm font-bold text-slate-900">Comprehensive Inventory Valuation</h4>
              <p class="text-[11px] text-slate-500">Source: <code>InventoryValue_View</code> & Projections</p>
            </div>
            <div class="flex gap-2">
              <span class="px-2 py-1 bg-slate-100 rounded text-xs font-mono font-bold text-slate-700">Cost: ${formatMoney(totalCost)}</span>
              <span class="px-2 py-1 bg-emerald-100 rounded text-xs font-mono font-bold text-emerald-800">Margin: ${formatMoney(totalMargin)}</span>
            </div>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 border-b border-slate-200 font-bold text-slate-600 uppercase">
                <tr>
                  <th class="py-2.5 px-3">Medicine</th>
                  <th class="py-2.5 px-3">Batch</th>
                  <th class="py-2.5 px-3 text-center">Stock</th>
                  <th class="py-2.5 px-3 text-right">Cost Price</th>
                  <th class="py-2.5 px-3 text-right">Valuation (Cost)</th>
                  <th class="py-2.5 px-3 text-right">Retail Price</th>
                  <th class="py-2.5 px-3 text-right">Proj. Gross Margin</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                ${data.map(r => `
                  <tr>
                    <td class="py-2.5 px-3 font-bold text-slate-900">${r.medicineName}</td>
                    <td class="py-2.5 px-3 font-mono text-slate-500">${r.batchNumber}</td>
                    <td class="py-2.5 px-3 text-center font-bold">${r.quantityInStock}</td>
                    <td class="py-2.5 px-3 text-right font-mono">${formatMoney(r.purchasePrice)}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-slate-900">${formatMoney(r.inventoryValuationAtCost)}</td>
                    <td class="py-2.5 px-3 text-right font-mono">${formatMoney(r.sellingPrice)}</td>
                    <td class="py-2.5 px-3 text-right font-mono font-bold text-emerald-700">${formatMoney(r.projectGrossMargin || r.projectedGrossMargin)}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `;
        break;
      }
    }
  } catch (err) {
    container.innerHTML = `<div class="p-8 text-center text-red-600 text-xs">Error loading report: ${err.message}</div>`;
  }
}

// =============================================================================
// MODULE 12: USERS (ADMIN ONLY)
// =============================================================================
async function renderUsers() {
  const users = await Api.getUsers();
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-base font-bold text-slate-800">User Accounts & Roles</h3>
        <p class="text-xs text-slate-500">Role-based access control (Admin, Pharmacist, Staff)</p>
      </div>
      <button id="btn-add-user" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg transition-colors flex items-center gap-2">
        <i data-lucide="user-plus" class="w-4 h-4"></i>
        <span>Create User Account</span>
      </button>
    </div>

    <div class="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider">
            <tr>
              <th class="py-3 px-4">User ID</th>
              <th class="py-3 px-4">Full Name</th>
              <th class="py-3 px-4">Username</th>
              <th class="py-3 px-4">Assigned Role</th>
              <th class="py-3 px-4">Phone</th>
              <th class="py-3 px-4">Email</th>
              <th class="py-3 px-4">Created Date</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            ${users.map(u => `
              <tr class="hover:bg-slate-50 transition-colors">
                <td class="py-3 px-4 font-mono font-bold text-slate-600">#USR-${u.userID}</td>
                <td class="py-3 px-4 font-bold text-slate-900">${u.fullName}</td>
                <td class="py-3 px-4 font-mono text-slate-700">${u.username}</td>
                <td class="py-3 px-4">
                  <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold ${u.role === 'Admin' ? 'bg-purple-100 text-purple-800' : 'bg-emerald-100 text-emerald-800'}">
                    ${u.role}
                  </span>
                </td>
                <td class="py-3 px-4 text-slate-600 font-mono">${u.phone || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-600">${u.email || 'N/A'}</td>
                <td class="py-3 px-4 text-slate-500">${formatDate(u.createdAt)}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `;

  lucide.createIcons();

  document.getElementById('btn-add-user').addEventListener('click', () => {
    openModal(`
      <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
        <h3 class="text-base font-bold text-slate-800">Create New System User</h3>
        <button onclick="closeModal()" class="text-slate-400 hover:text-slate-600"><i data-lucide="x" class="w-5 h-5"></i></button>
      </div>
      <form id="new-user-form" class="p-6 space-y-4">
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Full Name *</label>
          <input type="text" id="u-fullname" required placeholder="e.g. Dr. Shamsul Haque"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Username *</label>
            <input type="text" id="u-username" required placeholder="e.g. shamsul"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Role *</label>
            <select id="u-role" required class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs bg-white">
              <option value="Staff">Staff</option>
              <option value="Pharmacist">Pharmacist</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
        </div>
        <div>
          <label class="block text-xs font-semibold text-slate-700 mb-1">Initial Password *</label>
          <input type="password" id="u-password" required placeholder="Minimum 6 characters"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          <p class="text-[10px] text-slate-400 mt-1">Stored securely in Oracle using BCrypt password hashing.</p>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Phone</label>
            <input type="text" id="u-phone" placeholder="017xxxxxxxx"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Email</label>
            <input type="email" id="u-email" placeholder="user@pharmacy.com"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
        </div>
        <div id="u-modal-error" class="hidden p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-xs"></div>
        <div class="pt-4 border-t border-slate-200 flex justify-end gap-3">
          <button type="button" onclick="closeModal()" class="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Cancel</button>
          <button type="submit" class="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg">Create Account</button>
        </div>
      </form>
    `);

    document.getElementById('new-user-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      const errEl = document.getElementById('u-modal-error');
      try {
        await Api.createUser({
          fullName: document.getElementById('u-fullname').value.trim(),
          username: document.getElementById('u-username').value.trim(),
          role: document.getElementById('u-role').value,
          password: document.getElementById('u-password').value,
          phone: document.getElementById('u-phone').value.trim() || null,
          email: document.getElementById('u-email').value.trim() || null
        });
        showToast('User account created with BCrypt password hashing.', 'success');
        closeModal();
        navigate('users', true);
      } catch (err) {
        errEl.textContent = err.message;
        errEl.classList.remove('hidden');
      }
    });
  });
}

// =============================================================================
// MODULE 13: SETTINGS & PROFILE
// =============================================================================
async function renderSettings() {
  const user = Auth.getUser();
  const content = document.getElementById('content-area');

  content.innerHTML = `
    <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
      
      <!-- Account Profile Card -->
      <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-4">
        <h3 class="text-base font-bold text-slate-800 flex items-center gap-2">
          <i data-lucide="user-check" class="w-5 h-5 text-emerald-600"></i>
          <span>Account Profile</span>
        </h3>
        <div class="p-4 bg-slate-50 rounded-xl space-y-2 text-xs">
          <div class="flex justify-between py-1 border-b border-slate-200">
            <span class="text-slate-500">Full Name</span>
            <span class="font-bold text-slate-900">${user?.fullName || 'N/A'}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-slate-200">
            <span class="text-slate-500">Username</span>
            <span class="font-mono font-bold text-slate-900">${user?.username || 'N/A'}</span>
          </div>
          <div class="flex justify-between py-1 border-b border-slate-200">
            <span class="text-slate-500">System Role</span>
            <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800">${user?.role || 'Staff'}</span>
          </div>
          <div class="flex justify-between py-1">
            <span class="text-slate-500">User ID</span>
            <span class="font-mono text-slate-600">#${user?.userId || 1}</span>
          </div>
        </div>

        <h4 class="text-xs font-bold uppercase tracking-wider text-slate-500 pt-2">Security & Credentials</h4>
        <form id="change-pwd-form" class="space-y-3">
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">Current Password *</label>
            <input type="password" id="pwd-current" required placeholder="••••••••"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <div>
            <label class="block text-xs font-semibold text-slate-700 mb-1">New Password *</label>
            <input type="password" id="pwd-new" required minlength="6" placeholder="Minimum 6 characters"
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-xs focus:ring-2 focus:ring-emerald-500 focus:outline-none">
          </div>
          <button type="submit" class="w-full py-2 bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs rounded-lg transition-colors">
            Update Password
          </button>
        </form>
      </div>

      <!-- System Environment & Oracle Status Card -->
      <div class="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-4">
        <h3 class="text-base font-bold text-slate-800 flex items-center gap-2">
          <i data-lucide="server" class="w-5 h-5 text-emerald-600"></i>
          <span>Architecture & Database Status</span>
        </h3>

        <div class="p-4 bg-emerald-50/60 border border-emerald-200 rounded-xl space-y-2 text-xs">
          <div class="flex items-center justify-between">
            <span class="font-semibold text-emerald-900">Database Engine</span>
            <span class="font-mono font-bold text-emerald-700">Oracle Database 26ai Free</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="font-semibold text-emerald-900">Dedicated App User</span>
            <span class="font-mono text-emerald-700">C##PHARMACY_APP</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="font-semibold text-emerald-900">CDB/PDB Service</span>
            <span class="font-mono text-emerald-700">localhost:1521/FREE</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="font-semibold text-emerald-900">Schema Objects</span>
            <span class="font-bold text-emerald-800">12 Tables, 7 Views, 4 Procedures, 2 Functions, 6 Triggers</span>
          </div>
        </div>

        <div class="space-y-2 text-xs text-slate-600">
          <h4 class="font-bold text-slate-800">API Endpoint Configuration</h4>
          <p class="text-[11px] text-slate-500">Configure base API URL if running the frontend independently on a different port:</p>
          <div class="flex gap-2">
            <input type="text" id="api-base-input" value="${Api.getBaseUrl()}"
              class="flex-1 px-3 py-1.5 border border-slate-300 rounded-lg text-xs font-mono">
            <button onclick="saveApiUrl()" class="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs rounded-lg">
              Save
            </button>
          </div>
        </div>

        <div class="pt-4 border-t border-slate-200">
          <p class="text-[11px] text-slate-400">
            Al-Dawah Pharma Production Version 1.0.0. Clean Layered Architecture: Frontend &rarr; ASP.NET Core 10 Web API &rarr; Application &rarr; Infrastructure &rarr; Oracle 26ai.
          </p>
        </div>
      </div>

    </div>
  `;

  lucide.createIcons();

  document.getElementById('change-pwd-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const currentPassword = document.getElementById('pwd-current').value;
    const newPassword = document.getElementById('pwd-new').value;

    try {
      await Api.changePassword({ currentPassword, newPassword });
      showToast('Password changed successfully.', 'success');
      document.getElementById('change-pwd-form').reset();
    } catch (err) {
      showToast(err.message, 'error');
    }
  });

  window.saveApiUrl = () => {
    const url = document.getElementById('api-base-input').value.trim();
    Api.setBaseUrl(url);
    showToast(`API Base URL saved: ${url}`, 'success');
  };
}
