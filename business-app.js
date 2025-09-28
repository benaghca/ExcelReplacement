// Business Application JavaScript
class BusinessApp {
    constructor() {
        this.currentUser = null;
        this.apiBaseUrl = 'http://localhost:5000/api';
        this.init();
    }

    async init() {
        // Show loading screen
        this.showLoadingScreen();
        
        // Check if user is already logged in
        const token = localStorage.getItem('authToken');
        if (token && await this.validateToken(token)) {
            await this.loadUserData();
            this.showMainApp();
        } else {
            this.showLoginScreen();
        }
        
        this.setupEventListeners();
    }

    showLoadingScreen() {
        document.getElementById('loadingScreen').style.display = 'flex';
        document.getElementById('loginScreen').style.display = 'none';
        document.getElementById('registerScreen').style.display = 'none';
        document.getElementById('mainApp').style.display = 'none';
    }

    showLoginScreen() {
        document.getElementById('loadingScreen').style.display = 'none';
        document.getElementById('loginScreen').style.display = 'flex';
        document.getElementById('registerScreen').style.display = 'none';
        document.getElementById('mainApp').style.display = 'none';
    }

    showRegisterScreen() {
        document.getElementById('loadingScreen').style.display = 'none';
        document.getElementById('loginScreen').style.display = 'none';
        document.getElementById('registerScreen').style.display = 'flex';
        document.getElementById('mainApp').style.display = 'none';
    }

    showMainApp() {
        document.getElementById('loadingScreen').style.display = 'none';
        document.getElementById('loginScreen').style.display = 'none';
        document.getElementById('registerScreen').style.display = 'none';
        document.getElementById('mainApp').style.display = 'flex';
        
        // Load dashboard data
        this.loadDashboardData();
    }

    setupEventListeners() {
        // Login form
        document.getElementById('loginForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleLogin();
        });

        // Register form
        document.getElementById('registerForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleRegister();
        });

        // Navigation
        document.getElementById('showRegister').addEventListener('click', (e) => {
            e.preventDefault();
            this.showRegisterScreen();
        });

        document.getElementById('showLogin').addEventListener('click', (e) => {
            e.preventDefault();
            this.showLoginScreen();
        });

        // Sidebar navigation
        document.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleNavigation(link.getAttribute('href').substring(1));
            });
        });

        // Sidebar toggle
        document.getElementById('sidebarToggle').addEventListener('click', () => {
            document.querySelector('.sidebar').classList.toggle('collapsed');
        });

        // Logout
        document.getElementById('logoutBtn').addEventListener('click', () => {
            this.handleLogout();
        });

        // New document button
        document.getElementById('newDocumentBtn').addEventListener('click', () => {
            this.handleNewDocument();
        });
    }

    async handleLogin() {
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;

        try {
            const response = await fetch(`${this.apiBaseUrl}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ email, password })
            });

            const result = await response.json();

            if (result.success) {
                localStorage.setItem('authToken', result.token);
                this.currentUser = result.user;
                this.showMainApp();
            } else {
                this.showError(result.errorMessage || 'Login failed');
            }
        } catch (error) {
            this.showError('Network error. Please try again.');
        }
    }

    async handleRegister() {
        const formData = {
            email: document.getElementById('regEmail').value,
            password: document.getElementById('regPassword').value,
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            organizationName: document.getElementById('organizationName').value
        };

        const confirmPassword = document.getElementById('confirmPassword').value;

        if (formData.password !== confirmPassword) {
            this.showError('Passwords do not match');
            return;
        }

        try {
            const response = await fetch(`${this.apiBaseUrl}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                localStorage.setItem('authToken', result.token);
                this.currentUser = result.user;
                this.showMainApp();
            } else {
                this.showError(result.errorMessage || 'Registration failed');
            }
        } catch (error) {
            this.showError('Network error. Please try again.');
        }
    }

    async validateToken(token) {
        try {
            const response = await fetch(`${this.apiBaseUrl}/auth/validate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                }
            });

            return response.ok;
        } catch (error) {
            return false;
        }
    }

    async loadUserData() {
        const token = localStorage.getItem('authToken');
        try {
            const response = await fetch(`${this.apiBaseUrl}/auth/user`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                this.currentUser = await response.json();
                this.updateUserInfo();
            }
        } catch (error) {
            console.error('Failed to load user data:', error);
        }
    }

    updateUserInfo() {
        if (this.currentUser) {
            document.getElementById('userName').textContent = 
                `${this.currentUser.firstName} ${this.currentUser.lastName}`;
            document.getElementById('userRole').textContent = 
                this.currentUser.role?.name || 'User';
        }
    }

    handleNavigation(page) {
        // Update active nav item
        document.querySelectorAll('.nav-item').forEach(item => {
            item.classList.remove('active');
        });
        
        document.querySelector(`[href="#${page}"]`).parentElement.classList.add('active');

        // Update page content
        document.querySelectorAll('.page-content').forEach(content => {
            content.classList.remove('active');
        });

        const targetContent = document.getElementById(`${page}Content`);
        if (targetContent) {
            targetContent.classList.add('active');
        }

        // Update page title
        const pageTitles = {
            dashboard: 'Dashboard',
            templates: 'Templates',
            'data-sources': 'Data Sources',
            processing: 'Processing',
            analytics: 'Analytics',
            settings: 'Settings'
        };

        document.getElementById('pageTitle').textContent = pageTitles[page] || 'Page';
        document.getElementById('pageDescription').textContent = 
            `Manage your ${pageTitles[page].toLowerCase()} here`;

        // Load page-specific data
        this.loadPageData(page);
    }

    async loadPageData(page) {
        switch (page) {
            case 'dashboard':
                await this.loadDashboardData();
                break;
            case 'templates':
                await this.loadTemplatesData();
                break;
            case 'data-sources':
                await this.loadDataSourcesData();
                break;
            case 'processing':
                await this.loadProcessingData();
                break;
            case 'analytics':
                await this.loadAnalyticsData();
                break;
            case 'settings':
                await this.loadSettingsData();
                break;
        }
    }

    async loadDashboardData() {
        try {
            const token = localStorage.getItem('authToken');
            const response = await fetch(`${this.apiBaseUrl}/dashboard/stats`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const stats = await response.json();
                this.updateDashboardStats(stats);
            }
        } catch (error) {
            console.error('Failed to load dashboard data:', error);
        }
    }

    updateDashboardStats(stats) {
        document.getElementById('totalTemplates').textContent = stats.totalTemplates || 0;
        document.getElementById('totalDataSources').textContent = stats.totalDataSources || 0;
        document.getElementById('totalJobs').textContent = stats.totalJobs || 0;
        document.getElementById('documentsProcessed').textContent = stats.documentsProcessed || 0;
    }

    async loadTemplatesData() {
        // Load templates data
        console.log('Loading templates data...');
    }

    async loadDataSourcesData() {
        // Load data sources data
        console.log('Loading data sources data...');
    }

    async loadProcessingData() {
        // Load processing data
        console.log('Loading processing data...');
    }

    async loadAnalyticsData() {
        // Load analytics data
        console.log('Loading analytics data...');
    }

    async loadSettingsData() {
        // Load settings data
        console.log('Loading settings data...');
    }

    handleNewDocument() {
        // Open new document dialog
        console.log('Opening new document dialog...');
    }

    handleLogout() {
        localStorage.removeItem('authToken');
        this.currentUser = null;
        this.showLoginScreen();
    }

    showError(message) {
        // Simple error display - in production, use a proper notification system
        alert(message);
    }

    showSuccess(message) {
        // Simple success display - in production, use a proper notification system
        alert(message);
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new BusinessApp();
});

// Utility functions
function formatDate(date) {
    return new Date(date).toLocaleDateString();
}

function formatNumber(number) {
    return new Intl.NumberFormat().format(number);
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Export for use in other modules
window.BusinessApp = BusinessApp;
