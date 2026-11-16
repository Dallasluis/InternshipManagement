// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Site-wide JavaScript
document.addEventListener('DOMContentLoaded', function () {
    const loader = document.getElementById('app-global-loader');
    const toastContainer = document.getElementById('app-toast-container');

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
        showLoader(false);
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