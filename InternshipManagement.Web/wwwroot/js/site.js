// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Site-wide JavaScript
document.addEventListener('DOMContentLoaded', function () {
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