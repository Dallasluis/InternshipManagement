/* Education, experience, skills, certificates and projects: list, add and remove.
 * Text is inserted with textContent / value only, never innerHTML. */
(function () {
    'use strict';

    var root = document.getElementById('ai-root');
    if (!root || root.dataset.page !== 'sections') return;

    var tokenInput = root.querySelector('input[name="__RequestVerificationToken"]');
    var csrf = tokenInput ? tokenInput.value : '';
    var errors = document.getElementById('sections-errors');
    var host = document.getElementById('sections-root');

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }
    function clear(node) { while (node.firstChild) node.removeChild(node.firstChild); }

    function camelize(value) {
        if (Array.isArray(value)) return value.map(camelize);
        if (value && typeof value === 'object') {
            var out = {};
            Object.keys(value).forEach(function (k) { out[k.charAt(0).toLowerCase() + k.slice(1)] = camelize(value[k]); });
            return out;
        }
        return value;
    }

    // Reads the message out of the shapes the API uses: { error }, { message }, { errors: { Field: [..] } }, { title }.
    function messageFrom(status, data) {
        if (data) {
            if (typeof data.error === 'string') return data.error;
            if (typeof data.message === 'string') return data.message;
            if (data.errors && typeof data.errors === 'object') {
                var all = [];
                Object.keys(data.errors).forEach(function (k) {
                    var v = data.errors[k];
                    (Array.isArray(v) ? v : [v]).forEach(function (m) { if (typeof m === 'string') all.push(m); });
                });
                if (all.length) return all.join(' ');
            }
        }
        if (status === 401) return 'Your session has expired. Please log in again.';
        if (status === 403) return 'You cannot do this yet. Verify your email address first.';
        return 'Something went wrong. Please try again.';
    }

    async function api(url, options) {
        options = options || {};
        options.headers = options.headers || {};
        if (options.method && options.method !== 'GET') options.headers['RequestVerificationToken'] = csrf;
        options.credentials = 'same-origin';

        var response;
        try { response = await fetch(url, options); }
        catch (e) { throw new Error('Could not reach the server. Check your connection and try again.'); }

        var data = null;
        try { data = camelize(await response.json()); } catch (e) { data = null; }

        if (response.status === 401) {
            window.location.href = root.dataset.loginUrl;
            throw new Error('Your session has expired. Please log in again.');
        }
        if (!response.ok) throw new Error(messageFrom(response.status, data));
        return data;
    }

    function showError(message) {
        clear(errors);
        if (!message) return;
        var box = el('div', 'alert alert-danger', message);
        box.setAttribute('role', 'alert');
        errors.appendChild(box);
    }

    function day(value) { return value ? String(value).slice(0, 10) : ''; }
    function range(start, end, current) {
        var a = day(start), b = current ? 'present' : day(end);
        return a ? (a + ' – ' + (b || '…')) : '';
    }

    var LEVELS = ['Beginner', 'Intermediate', 'Advanced'];

    // Each section: where it lives in the profile, how an item is shown, and which fields the add form has.
    var SECTIONS = [
        {
            key: 'education', title: 'Education', list: 'education', icon: 'fa-graduation-cap',
            label: function (i) { return [i.degree, i.fieldOfStudy].filter(Boolean).join(' in ') + ' at ' + i.institution; },
            sub: function (i) { return [range(i.startDate, i.endDate, i.isCurrent), i.grade, i.completionStatus].filter(Boolean).join(' · '); },
            fields: [
                { name: 'institution', label: 'Institution', required: true, max: 200 },
                { name: 'degree', label: 'Degree or qualification', required: true, max: 200 },
                { name: 'fieldOfStudy', label: 'Field of study', required: true, max: 200 },
                { name: 'startDate', label: 'Start date', type: 'date', required: true },
                { name: 'endDate', label: 'End date', type: 'date' },
                { name: 'isCurrent', label: 'I am still studying here', type: 'checkbox' },
                { name: 'grade', label: 'Grade (optional)', max: 50 },
                { name: 'completionStatus', label: 'Status (optional)', placeholder: 'e.g., Completed, In progress', max: 50 }
            ]
        },
        {
            key: 'experience', title: 'Work experience', list: 'workExperience', icon: 'fa-briefcase',
            label: function (i) { return i.position + ' at ' + i.company; },
            sub: function (i) { return [range(i.startDate, i.endDate, i.isCurrent), i.description].filter(Boolean).join(' · '); },
            fields: [
                { name: 'company', label: 'Company', required: true, max: 200 },
                { name: 'position', label: 'Position', required: true, max: 200 },
                { name: 'description', label: 'What you did (optional)', type: 'textarea', max: 1000 },
                { name: 'startDate', label: 'Start date', type: 'date', required: true },
                { name: 'endDate', label: 'End date', type: 'date' },
                { name: 'isCurrent', label: 'I still work here', type: 'checkbox' }
            ]
        },
        {
            key: 'skill', title: 'Skills', list: 'skills', icon: 'fa-tools',
            label: function (i) { return i.name + (i.proficiencyLevel ? ' (' + i.proficiencyLevel + ')' : ''); },
            sub: function () { return ''; },
            fields: [
                { name: 'name', label: 'Skill', required: true, max: 100 },
                { name: 'proficiencyLevel', label: 'Level', type: 'select', options: LEVELS }
            ]
        },
        {
            key: 'certification', title: 'Certificates', list: 'certifications', icon: 'fa-certificate',
            label: function (i) { return i.name + (i.issuingOrganization ? ' — ' + i.issuingOrganization : ''); },
            sub: function (i) { return [day(i.issueDate) && 'Issued ' + day(i.issueDate), day(i.expirationDate) && 'Expires ' + day(i.expirationDate)].filter(Boolean).join(' · '); },
            fields: [
                { name: 'name', label: 'Certificate name', required: true, max: 200 },
                { name: 'issuingOrganization', label: 'Issued by', required: true, max: 200 },
                { name: 'issueDate', label: 'Issue date', type: 'date' },
                { name: 'expirationDate', label: 'Expiry date', type: 'date' }
            ]
        },
        {
            key: 'project', title: 'Projects', list: 'projects', icon: 'fa-code-branch',
            label: function (i) { return i.title + (i.role ? ' — ' + i.role : ''); },
            sub: function (i) { return [i.technologies, i.description].filter(Boolean).join(' · '); },
            fields: [
                { name: 'title', label: 'Project title', required: true, max: 200 },
                { name: 'role', label: 'Your role (optional)', max: 100 },
                { name: 'technologies', label: 'Technologies (optional)', placeholder: 'e.g., C#, SQL, React', max: 300 },
                { name: 'description', label: 'Description (optional)', type: 'textarea', max: 1000 },
                { name: 'startDate', label: 'Start date', type: 'date' },
                { name: 'endDate', label: 'End date', type: 'date' }
            ]
        }
    ];

    function buildForm(section, onSaved) {
        var form = el('form', 'border rounded p-3 mt-3 bg-light d-none');
        form.noValidate = true;
        var inputs = {};
        var row = el('div', 'row g-2');

        section.fields.forEach(function (f) {
            var col = el('div', f.type === 'textarea' ? 'col-12' : (f.type === 'checkbox' ? 'col-12' : 'col-md-6'));
            var id = 'f-' + section.key + '-' + f.name;
            var input;

            if (f.type === 'checkbox') {
                var wrap = el('div', 'form-check');
                input = el('input', 'form-check-input');
                input.type = 'checkbox';
                input.id = id;
                var lbl = el('label', 'form-check-label', f.label);
                lbl.htmlFor = id;
                wrap.appendChild(input);
                wrap.appendChild(lbl);
                col.appendChild(wrap);
            } else {
                var label = el('label', 'form-label small mb-1', f.label + (f.required ? ' *' : ''));
                label.htmlFor = id;
                col.appendChild(label);
                if (f.type === 'textarea') {
                    input = el('textarea', 'form-control form-control-sm');
                    input.rows = 2;
                } else if (f.type === 'select') {
                    input = el('select', 'form-select form-select-sm');
                    var blank = el('option', null, 'Choose…');
                    blank.value = '';
                    input.appendChild(blank);
                    f.options.forEach(function (o) { var opt = el('option', null, o); opt.value = o; input.appendChild(opt); });
                } else {
                    input = el('input', 'form-control form-control-sm');
                    input.type = f.type || 'text';
                }
                input.id = id;
                if (f.placeholder) input.placeholder = f.placeholder;
                if (f.max && f.type !== 'select') input.maxLength = f.max;
                col.appendChild(input);
            }
            inputs[f.name] = input;
            row.appendChild(col);
        });
        form.appendChild(row);

        var message = el('div', 'mt-2');
        var save = el('button', 'btn btn-primary btn-sm mt-2', 'Add');
        save.type = 'submit';
        var cancel = el('button', 'btn btn-link btn-sm mt-2', 'Cancel');
        cancel.type = 'button';
        cancel.addEventListener('click', function () { form.classList.add('d-none'); form.reset(); clear(message); });
        form.appendChild(message);
        form.appendChild(save);
        form.appendChild(cancel);

        form.addEventListener('submit', async function (ev) {
            ev.preventDefault();
            clear(message);
            var payload = {};
            for (var i = 0; i < section.fields.length; i++) {
                var f = section.fields[i];
                var input = inputs[f.name];
                var value;
                if (f.type === 'checkbox') value = input.checked;
                else {
                    value = input.value.trim();
                    if (f.required && !value) {
                        message.appendChild(el('div', 'text-danger small', f.label + ' is required.'));
                        input.focus();
                        return;
                    }
                    if (value === '') value = null;
                }
                payload[f.name] = value;
            }

            save.disabled = true;
            save.textContent = 'Saving…';
            try {
                await api(root.dataset.addUrl + '?section=' + encodeURIComponent(section.key), {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                await onSaved();
            } catch (e) {
                message.appendChild(el('div', 'text-danger small', e.message));
                save.disabled = false;
                save.textContent = 'Add';
            }
        });

        return form;
    }

    function renderSection(section, profile, reload) {
        var items = (profile[section.list] || []);
        var card = el('div', 'card mb-3');
        var body = el('div', 'card-body');

        var head = el('div', 'd-flex justify-content-between align-items-center');
        var title = el('h2', 'h5 mb-0');
        title.appendChild(el('i', 'fas ' + section.icon + ' me-2 text-primary'));
        title.appendChild(document.createTextNode(section.title));
        head.appendChild(title);
        var toggle = el('button', 'btn btn-outline-primary btn-sm', '+ Add');
        toggle.type = 'button';
        head.appendChild(toggle);
        body.appendChild(head);

        if (!items.length) {
            body.appendChild(el('p', 'text-muted small mt-3 mb-0', 'Nothing added yet.'));
        } else {
            var list = el('ul', 'list-group list-group-flush mt-3');
            items.forEach(function (item) {
                var li = el('li', 'list-group-item px-0 d-flex justify-content-between align-items-start gap-2');
                var text = el('div');
                text.appendChild(el('div', 'fw-semibold', section.label(item)));
                var sub = section.sub(item);
                if (sub) text.appendChild(el('div', 'small text-muted', sub));
                li.appendChild(text);

                var remove = el('button', 'btn btn-sm btn-outline-danger', 'Remove');
                remove.type = 'button';
                remove.addEventListener('click', async function () {
                    if (!window.confirm('Remove "' + section.label(item) + '" from your profile?')) return;
                    showError('');
                    remove.disabled = true;
                    try {
                        await api(root.dataset.removeUrl + '?section=' + encodeURIComponent(section.key) + '&id=' + encodeURIComponent(item.id), { method: 'POST' });
                        await reload();
                    } catch (e) {
                        showError(e.message);
                        remove.disabled = false;
                    }
                });
                li.appendChild(remove);
                list.appendChild(li);
            });
            body.appendChild(list);
        }

        var form = buildForm(section, reload);
        toggle.addEventListener('click', function () {
            form.classList.toggle('d-none');
            if (!form.classList.contains('d-none')) {
                var first = form.querySelector('input,textarea,select');
                if (first) first.focus();
            }
        });
        body.appendChild(form);
        card.appendChild(body);
        return card;
    }

    async function load() {
        showError('');
        try {
            var profile = await api(root.dataset.profileUrl);
            clear(host);
            SECTIONS.forEach(function (s) { host.appendChild(renderSection(s, profile || {}, load)); });
        } catch (e) {
            clear(host);
            showError(e.message);
        }
    }

    load();
})();
