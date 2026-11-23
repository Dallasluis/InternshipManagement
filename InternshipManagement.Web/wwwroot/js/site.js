// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Site-wide JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const loader = document.getElementById('app-global-loader');
    const toastContainer = document.getElementById('app-toast-container');
    const themeToggle = document.getElementById('theme-toggle');
    const themeIcon = themeToggle && themeToggle.querySelector('i');
    const themeLabel = themeToggle && themeToggle.querySelector('span');
    let navigationTimer;

    function showLoader(show) {
        if (loader) {
            loader.classList.toggle('d-none', !show);
        }
    }

    function showToast(message, tone) {
        if (!toastContainer || !message) return;
        const toast = document.createElement('div');
        toast.className = 'toast align-items-center border-0 show';
        if (tone === 'success') toast.classList.add('text-bg-success');
        else if (tone === 'danger') toast.classList.add('text-bg-danger');
        else toast.classList.add('text-bg-dark');
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');

        const content = document.createElement('div');
        content.className = 'd-flex';
        const body = document.createElement('div');
        body.className = 'toast-body';
        body.textContent = String(message);
        const closeButton = document.createElement('button');
        closeButton.type = 'button';
        closeButton.className = 'btn-close btn-close-white me-2 m-auto';
        closeButton.setAttribute('aria-label', 'Close');
        closeButton.addEventListener('click', () => toast.remove());
        content.appendChild(body);
        content.appendChild(closeButton);
        toast.appendChild(content);
        toastContainer.appendChild(toast);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 250);
        }, 3500);
    }

    window.showFriendlyMessage = function (message, tone) {
        showToast(message || 'Something went wrong. Please try again in a moment.', tone || 'info');
    };

    function setTheme(theme) {
        document.documentElement.dataset.theme = theme;
        if (themeToggle) {
            const isDark = theme === 'dark';
            themeToggle.setAttribute('aria-pressed', String(isDark));
            if (themeLabel) themeLabel.textContent = isDark ? 'Light mode' : 'Dark mode';
            if (themeIcon) themeIcon.className = 'fas ' + (isDark ? 'fa-sun' : 'fa-moon') + ' me-2';
        }
    }

    if (themeToggle) {
        setTheme(document.documentElement.dataset.theme || 'light');
        themeToggle.addEventListener('click', function () {
            const nextTheme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
            setTheme(nextTheme);
            try {
                localStorage.setItem('internshiphub-theme', nextTheme);
            } catch (error) {
                window.showFriendlyMessage('Your theme changed, but this browser could not save the preference.', 'danger');
            }
        });
    }

    document.addEventListener('click', function (event) {
        const link = event.target.closest('a[href]');
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        if (link.target && link.target !== '_self' || link.hasAttribute('download') || link.hasAttribute('data-bs-toggle')) return;

        const destination = new URL(link.href, window.location.href);
        if (destination.origin !== window.location.origin || destination.pathname === window.location.pathname && destination.search === window.location.search && destination.hash) return;
        if (destination.pathname === window.location.pathname && destination.search === window.location.search) return;

        window.clearTimeout(navigationTimer);
        navigationTimer = window.setTimeout(function () { showLoader(true); }, 140);
    });

    document.querySelectorAll('form').forEach(function (form) {
        if (form.dataset.noLoading === 'true') return;
        form.addEventListener('submit', function () {
            const submitButton = form.querySelector('button[type="submit"], input[type="submit"]');
            if (submitButton) {
                if (!submitButton.dataset.originalText) {
                    submitButton.dataset.originalText = submitButton.textContent.trim() || submitButton.value || 'Submit';
                }
                submitButton.disabled = true;
                submitButton.dataset.loadingText = submitButton.dataset.loadingText || 'Please wait...';
                submitButton.textContent = submitButton.dataset.loadingText;
            }
            showLoader(true);
        });
    });

    window.addEventListener('beforeunload', function () {
        window.clearTimeout(navigationTimer);
    });

    console.log('InternshipHub loaded');
});

// Function to toggle company fields on registration
function toggleCompanyFields() {
    var userType = document.querySelector('#UserType');
    var companyFields = document.querySelector('#companyFields');
    if (userType && companyFields) {
        companyFields.style.display = userType.value === 'Company' ? 'block' : 'none';
    }
}