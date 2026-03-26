// Authentication utilities for all pages

let isRedirecting = false; // Prevent multiple redirects
let authCheckComplete = false; // Track if auth check is done
let sessionTimeoutId = null; // Store timeout ID
let lastActivityTime = null; // Track last user activity

const SESSION_TIMEOUT = 15 * 60 * 1000; // 15 minutes in milliseconds
const ACTIVITY_CHECK_INTERVAL = 60 * 1000; // Check every minute

// Check if user is authenticated
function checkAuth() {
    // Prevent redirect loop
    if (isRedirecting) {
        return null;
    }
    
    // If already checked and we're still here, don't check again
    if (authCheckComplete) {
        return getCurrentUser();
    }
    
    const token = localStorage.getItem('authToken');
    const currentUser = localStorage.getItem('currentUser');
    const loginTime = localStorage.getItem('loginTime');
    
    if (!token || !currentUser) {
        // Save current page for redirect after login
        const currentPage = window.location.pathname.split('/').pop();
        const currentPath = window.location.pathname;
        
        // Don't redirect if already on login page
        if (currentPage !== 'login.html' && currentPage !== 'auth-test.html' && currentPath !== '/login.html') {
            isRedirecting = true;
            console.log('No auth token, redirecting to login...');
            window.location.replace(`/login.html?redirect=${currentPage}`);
        }
        return null;
    }
    
    // Check if session has expired
    if (loginTime) {
        const loginDate = new Date(loginTime);
        const now = new Date();
        const timeSinceLogin = now - loginDate;
        
        if (timeSinceLogin > SESSION_TIMEOUT) {
            console.log('Session expired, logging out...');
            alert('Your session has expired. Please login again.');
            logout();
            return null;
        }
    }
    
    try {
        const user = JSON.parse(currentUser);
        authCheckComplete = true;
        
        // Initialize session timeout
        initSessionTimeout();
        
        return user;
    } catch (e) {
        console.error('Invalid user data in localStorage:', e);
        localStorage.removeItem('authToken');
        localStorage.removeItem('currentUser');
        localStorage.removeItem('loginTime');
        if (!isRedirecting) {
            isRedirecting = true;
            window.location.replace('/login.html');
        }
        return null;
    }
}

// Initialize session timeout
function initSessionTimeout() {
    // Clear existing timeout if any
    if (sessionTimeoutId) {
        clearTimeout(sessionTimeoutId);
    }
    
    // Set last activity time
    lastActivityTime = new Date();
    
    // Set timeout for 15 minutes
    sessionTimeoutId = setTimeout(() => {
        alert('Your session has expired due to inactivity. Please login again.');
        logout();
    }, SESSION_TIMEOUT);
    
    // Update activity time on user interactions
    resetActivityTimer();
}

// Reset activity timer on user interaction
function resetActivityTimer() {
    // Remove existing listeners
    document.removeEventListener('mousedown', updateActivity);
    document.removeEventListener('keydown', updateActivity);
    document.removeEventListener('scroll', updateActivity);
    document.removeEventListener('touchstart', updateActivity);
    
    // Add new listeners
    document.addEventListener('mousedown', updateActivity);
    document.addEventListener('keydown', updateActivity);
    document.addEventListener('scroll', updateActivity);
    document.addEventListener('touchstart', updateActivity);
}

// Update last activity time
function updateActivity() {
    lastActivityTime = new Date();
    localStorage.setItem('lastActivityTime', lastActivityTime.toISOString());
    
    // Reset the timeout
    if (sessionTimeoutId) {
        clearTimeout(sessionTimeoutId);
        sessionTimeoutId = setTimeout(() => {
            alert('Your session has expired due to inactivity. Please login again.');
            logout();
        }, SESSION_TIMEOUT);
    }
}

// Check session timeout periodically
function checkSessionTimeout() {
    const loginTime = localStorage.getItem('loginTime');
    if (!loginTime) return;
    
    const loginDate = new Date(loginTime);
    const now = new Date();
    const timeSinceLogin = now - loginDate;
    
    if (timeSinceLogin > SESSION_TIMEOUT) {
        alert('Your session has expired. Please login again.');
        logout();
    }
}

// Start periodic session check
setInterval(checkSessionTimeout, ACTIVITY_CHECK_INTERVAL);

// Get auth token
function getAuthToken() {
    return localStorage.getItem('authToken');
}

// Get current user
function getCurrentUser() {
    const userStr = localStorage.getItem('currentUser');
    if (!userStr) return null;

    try {
        return JSON.parse(userStr);
    } catch (e) {
        return null;
    }
}

// Check if user has specific permission
function hasPermission(permission) {
    const user = getCurrentUser();
    if (!user || !user.roles) return false;

    // Admin has all permissions
    if (user.roles.includes('Admin')) return true;

    // Editor has read/write
    if (permission === 'Read' && (user.roles.includes('Editor') || user.roles.includes('Admin'))) {
        return true;
    }

    if (permission === 'ReadWrite' && user.roles.includes('Admin')) {
        return true;
    }

    // Viewer has only read
    if (permission === 'Read' && user.roles.includes('Viewer')) {
        return true;
    }

    return false;
}

// Logout function
function logout() {
    isRedirecting = true;
    
    // Clear session timeout
    if (sessionTimeoutId) {
        clearTimeout(sessionTimeoutId);
        sessionTimeoutId = null;
    }
    
    // Clear all auth data
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    localStorage.removeItem('loginTime');
    localStorage.removeItem('lastActivityTime');
    
    window.location.replace('login.html');
}

// Add auth header to fetch requests
function fetchWithAuth(url, options = {}) {
    const token = getAuthToken();

    if (!options.headers) {
        options.headers = {};
    }

    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    return fetch(url, options);
}

// Initialize auth UI elements (call this on page load)
function initAuthUI() {
    const user = getCurrentUser();
    
    // Update user info in header if element exists
    const userInfoEl = document.getElementById('userInfo');
    if (userInfoEl && user) {
        const userRoles = user.roles.join(', ');
        
        userInfoEl.innerHTML = `
            <div style="display: flex; align-items: center; gap: 12px;">
                <div style="text-align: right;">
                    <div style="font-weight: 600; font-size: 14px; color: #000;">${escapeHtml(user.username)}</div>
                    <div style="font-size: 12px; color: #666;">${escapeHtml(userRoles)} <span id="sessionTimer" style="margin-left: 8px;"></span></div>
                </div>
                <button onclick="logout()" class="btn btn-secondary" style="padding: 8px 16px; font-size: 13px;">
                    Logout
                </button>
            </div>
        `;
        
        // Start session timer display
        updateSessionTimer();
        setInterval(updateSessionTimer, 30000); // Update every 30 seconds
    }
    
    // Hide/show elements based on permissions
    hideElementsWithoutPermission();
}

// Update session timer display
function updateSessionTimer() {
    const timerEl = document.getElementById('sessionTimer');
    if (!timerEl) return;
    
    const timeRemaining = getSessionTimeRemaining();
    
    if (timeRemaining <= 0) {
        return; // Will be logged out by checkSessionTimeout
    }
    
    const minutes = Math.floor(timeRemaining / 60000);
    const seconds = Math.floor((timeRemaining % 60000) / 1000);
    
    timerEl.innerHTML = `${minutes}m ${seconds}s`;
}

// Helper function to get remaining session time
function getSessionTimeRemaining() {
    const loginTime = localStorage.getItem('loginTime');
    if (!loginTime) return 0;
    
    const loginDate = new Date(loginTime);
    const now = new Date();
    const timeSinceLogin = now - loginDate;
    
    return Math.max(0, SESSION_TIMEOUT - timeSinceLogin);
}

// Hide elements without required permissions
function hideElementsWithoutPermission() {
    const elements = document.querySelectorAll('[data-permission]');
    
    elements.forEach(el => {
        const requiredPermission = el.getAttribute('data-permission');
        
        if (!hasPermission(requiredPermission)) {
            el.style.display = 'none';
        } else {
            el.style.display = '';
        }
    });
}

// Escape HTML for safer output
function escapeHtml(html) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(html));
    return div.innerHTML;
}

// Show session timeout warning
function showSessionWarning(timeRemaining) {
    const minutes = Math.floor(timeRemaining / 60000);
    const seconds = Math.floor((timeRemaining % 60000) / 1000);
    
    // Check if warning already exists
    if (document.getElementById('sessionWarning')) return;
    
    const warning = document.createElement('div');
    warning.id = 'sessionWarning';
    warning.style.cssText = `
        position: fixed;
        top: 70px;
        right: 20px;
        background: #fff3cd;
        color: #856404;
        padding: 16px 20px;
        border-radius: 8px;
        border-left: 4px solid #ffc107;
        box-shadow: 0 4px 12px rgba(0,0,0,0.2);
        z-index: 9999;
        font-size: 14px;
        max-width: 350px;
        animation: slideInRight 0.3s ease;
    `;
    warning.innerHTML = `
        <div style="font-weight: 600; margin-bottom: 8px;">?? Session Expiring Soon</div>
        <div style="margin-bottom: 12px;">Your session will expire in ${minutes}m ${seconds}s</div>
        <div style="display: flex; gap: 8px;">
            <button onclick="extendSession()" style="padding: 6px 14px; border: none; background: #856404; color: white; border-radius: 4px; cursor: pointer; font-size: 13px; font-weight: 600;">
                Stay Logged In
            </button>
            <button onclick="this.parentElement.parentElement.remove()" style="padding: 6px 14px; border: 1px solid #856404; background: white; color: #856404; border-radius: 4px; cursor: pointer; font-size: 13px;">
                Dismiss
            </button>
        </div>
    `;
    
    document.body.appendChild(warning);
}

// Extend session (refresh login time)
function extendSession() {
    localStorage.setItem('loginTime', new Date().toISOString());
    localStorage.setItem('lastActivityTime', new Date().toISOString());
    
    // Remove warning
    const warning = document.getElementById('sessionWarning');
    if (warning) warning.remove();
    
    // Reinitialize timeout
    initSessionTimeout();
    
    // Update timer display
    updateSessionTimer();
    
    // Show confirmation
    const confirmation = document.createElement('div');
    confirmation.style.cssText = `
        position: fixed;
        top: 70px;
        right: 20px;
        background: #d4edda;
        color: #155724;
        padding: 12px 20px;
        border-radius: 8px;
        border-left: 4px solid #28a745;
        box-shadow: 0 4px 12px rgba(0,0,0,0.2);
        z-index: 9999;
        font-size: 14px;
        animation: slideInRight 0.3s ease;
    `;
    confirmation.innerHTML = `
        <div style="font-weight: 600;">? Session Extended</div>
        <div style="font-size: 13px; margin-top: 4px;">You have 15 more minutes</div>
    `;
    
    document.body.appendChild(confirmation);
    
    setTimeout(() => {
        confirmation.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => confirmation.remove(), 300);
    }, 3000);
}
