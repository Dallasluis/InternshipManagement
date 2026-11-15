/* Offers the administrator-managed category list as suggestions on any input marked data-categories.
   The box stays free text, so nothing breaks if the list cannot be loaded. */
(function () {
    'use strict';
    var inputs = document.querySelectorAll('input[data-categories]');
    if (!inputs.length) return;

    fetch('/Categories/List', { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function (names) {
            if (!Array.isArray(names) || !names.length) return;
            var list = document.createElement('datalist');
            list.id = 'category-suggestions';
            names.forEach(function (name) {
                var option = document.createElement('option');
                option.value = name;
                list.appendChild(option);
            });
            document.body.appendChild(list);
            inputs.forEach(function (input) { input.setAttribute('list', list.id); });
        })
        .catch(function () { /* plain text box */ });
})();
