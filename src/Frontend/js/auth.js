/**
 * Al-Dawah Pharma - Authentication Service
 */

const Auth = (() => {
  let currentUser = null;

  try {
    const stored = localStorage.getItem('aldawah_user');
    if (stored) {
      currentUser = JSON.parse(stored);
    }
  } catch (e) {
    currentUser = null;
  }

  return {
    getUser: () => currentUser,
    isAuthenticated: () => !!localStorage.getItem('aldawah_token') && !!currentUser,
    isAdmin: () => currentUser?.role === 'Admin',
    isStaff: () => currentUser?.role === 'Staff' || currentUser?.role === 'Pharmacist',
    hasRole: (...roles) => roles.includes(currentUser?.role),

    async login(username, password) {
      const result = await Api.login({ username, password });
      currentUser = {
        userId: result.userID,
        username: result.username,
        fullName: result.fullName,
        role: result.role,
      };

      localStorage.setItem('aldawah_token', result.token);
      localStorage.setItem('aldawah_user', JSON.stringify(currentUser));
      window.dispatchEvent(new CustomEvent('auth:login', { detail: currentUser }));
      return currentUser;
    },

    logout() {
      currentUser = null;
      localStorage.removeItem('aldawah_token');
      localStorage.removeItem('aldawah_user');
      window.dispatchEvent(new CustomEvent('auth:logout'));
    },

    async checkSession() {
      if (!this.isAuthenticated()) return null;
      try {
        const user = await Api.getMe();
        currentUser = {
          userId: user.userID,
          username: user.username,
          fullName: user.fullName,
          role: user.role,
        };
        localStorage.setItem('aldawah_user', JSON.stringify(currentUser));
        return currentUser;
      } catch (err) {
        this.logout();
        return null;
      }
    }
  };
})();
