/* Company AI tools: AI internship draft (on the Post Internship form) and best candidates page.
 * Everything the AI wrote is inserted with textContent or input values, never innerHTML. */
(function () {
    'use strict';

    var root = document.getElementById('ai-root');
    if (!root) return;

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var csrf = tokenInput ? tokenInput.value : '';
    var page = root.dataset.page;

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

    function friendlyStatus(status, data) {
        if (data && data.error) return data.error;
        if (status === 429) return 'You have used all your AI requests for now. Please try again in a while.';
        if (status === 502 || status === 503 || status === 504) return 'The AI service is unavailable right now. Try again, or fill in the form yourself.';
        return 'Something went wrong. Please try again.';
    }

    async function api(url, options) {
        options = options || {};
        var headers = options.headers || {};
        if (options.method && options.method !== 'GET') headers['RequestVerificationToken'] = csrf;
        options.headers = headers;
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
        if (!response.ok) throw new Error(friendlyStatus(response.status, data));
        return data;
    }

    function showError(container, message) {
        clear(container);
        if (!message) return;
        var box = el('div', 'alert alert-danger mb-2', message);
        box.setAttribute('role', 'alert');
        container.appendChild(box);
    }

    // ------------------------------------------------------------------ AI draft on the Post Internship form

    function field(id) { return document.getElementById(id); }
    function setValue(id, value) {
        var input = field(id);
        if (!input || value === null || value === undefined || value === '') return;
        input.value = value;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }
    function setSelect(id, value) {
        var select = field(id);
        if (!select || !value) return;
        for (var i = 0; i < select.options.length; i++) {
            if (select.options[i].value.toLowerCase() === String(value).toLowerCase()) {
                select.value = select.options[i].value;
                return;
            }
        }
    }
    function dateOnly(value) { return value ? String(value).slice(0, 10) : ''; }

    function applyDraft(draft, result) {
        var hasText = (field('Title') && field('Title').value.trim()) || (field('Description') && field('Description').value.trim());
        if (hasText && !window.confirm('This replaces what you have already typed in the form. Continue?')) return;

        setValue('Title', draft.title);
        setValue('Description', draft.description);
        setValue('Responsibilities', draft.responsibilities);
        setValue('RequiredQualifications', draft.requiredQualifications);
        setValue('PreferredQualifications', draft.preferredQualifications);
        setValue('RequiredSkills', draft.requiredSkills);
        setValue('PreferredSkills', draft.preferredSkills);
        setValue('Industry', draft.industry);
        setValue('Location', draft.location);
        setValue('EligibleProgrammes', (draft.eligibleProgrammes || []).join(', '));
        setSelect('Duration', draft.duration);
        if (draft.isRemote) setSelect('WorkArrangement', 'Remote');
        if (draft.startDate) setValue('StartDate', dateOnly(draft.startDate));
        if (draft.applicationDeadline) setValue('ApplicationDeadline', dateOnly(draft.applicationDeadline));
        if (draft.numberOfPositions) setValue('NumberOfPositions', draft.numberOfPositions);

        clear(result);
        var note = el('div', 'ai-note');
        note.appendChild(el('div', 'ai-note-label', 'AI draft added to the form'));
        note.appendChild(el('div', null, 'Read every field and correct anything that is wrong before you create the internship.'));

        var todo = (draft.fieldsToComplete || []).concat(draft.missingDetails || []);
        if (todo.length) {
            note.appendChild(el('div', 'fw-semibold mt-2', 'Still to complete'));
            var list = el('ul', 'mb-0');
            todo.forEach(function (t) { list.appendChild(el('li', null, t)); });
            note.appendChild(list);
        }
        result.appendChild(note);
        var title = field('Title');
        if (title) title.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function initDraft() {
        var prompt = document.getElementById('draft-prompt');
        var button = document.getElementById('draft-btn');
        var saved = document.getElementById('draft-saved');
        var errors = document.getElementById('draft-errors');
        var result = document.getElementById('draft-result');
        var count = document.getElementById('draft-count');

        prompt.addEventListener('input', function () { count.textContent = prompt.value.length + ' / 4000'; });

        function loadSaved() {
            api(root.dataset.draftsUrl).then(function (list) {
                if (!Array.isArray(list)) return;
                while (saved.options.length > 1) saved.remove(1);
                list.forEach(function (d) {
                    var opt = el('option', null, d.title || ('Draft ' + d.id));
                    opt.value = d.id;
                    saved.appendChild(opt);
                });
                saved.classList.toggle('d-none', list.length === 0);
            }).catch(function () { saved.classList.add('d-none'); });
        }

        button.addEventListener('click', async function () {
            showError(errors, '');
            clear(result);
            var text = prompt.value.trim();
            if (text.length < 20) { showError(errors, 'Describe the opportunity in at least 20 characters.'); return; }

            button.disabled = true;
            button.textContent = 'Writing your draft…';
            try {
                var draft = await api(root.dataset.generateUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ prompt: text })
                });
                applyDraft(draft, result);
                loadSaved();
            } catch (e) {
                showError(errors, e.message);
            } finally {
                button.disabled = false;
                button.textContent = 'Generate draft';
            }
        });

        saved.addEventListener('change', async function () {
            if (!saved.value) return;
            showError(errors, '');
            try {
                var draft = await api(root.dataset.draftUrl + '?id=' + encodeURIComponent(saved.value));
                applyDraft(draft, result);
            } catch (e) {
                showError(errors, e.message);
            } finally {
                saved.value = '';
            }
        });

        loadSaved();
    }

    // ------------------------------------------------------------------ best candidates page

    function scoreClass(score) {
        if (score >= 75) return 'bg-success';
        if (score >= 50) return 'bg-warning text-dark';
        return 'bg-secondary';
    }

    function badges(list, className) {
        var wrap = el('div', 'd-flex flex-wrap gap-1 mb-2');
        (list || []).forEach(function (item) { wrap.appendChild(el('span', 'badge ' + className, item)); });
        return wrap;
    }

    function initCandidates() {
        var list = document.getElementById('candidates-list');
        var errors = document.getElementById('candidates-errors');

        api(root.dataset.candidatesUrl + '?internshipId=' + encodeURIComponent(root.dataset.internshipId) + '&maxResults=15')
            .then(function (data) {
                clear(list);
                if (!Array.isArray(data) || !data.length) {
                    list.appendChild(el('div', 'text-muted py-4 text-center',
                        'No matching students yet. Candidates appear as students complete their profiles.'));
                    return;
                }
                data.forEach(function (m) {
                    var card = el('div', 'card mb-3');
                    var body = el('div', 'card-body');
                    var head = el('div', 'd-flex justify-content-between align-items-start mb-2');
                    var titleBox = el('div');
                    titleBox.appendChild(el('div', 'h5 mb-0', m.title));
                    if (m.subtitle) titleBox.appendChild(el('div', 'text-muted', m.subtitle));
                    head.appendChild(titleBox);
                    head.appendChild(el('span', 'badge fs-6 ' + scoreClass(m.matchScore), m.matchScore + '% match'));
                    body.appendChild(head);

                    if (m.matchingSkills && m.matchingSkills.length) {
                        body.appendChild(el('div', 'small text-muted mb-1', 'Matching skills'));
                        body.appendChild(badges(m.matchingSkills, 'bg-success-subtle text-success-emphasis border'));
                    }
                    if (m.missingPreferredSkills && m.missingPreferredSkills.length) {
                        body.appendChild(el('div', 'small text-muted mb-1', 'Missing nice-to-have skills'));
                        body.appendChild(badges(m.missingPreferredSkills, 'bg-light text-dark border'));
                    }
                    if (m.explanation) body.appendChild(el('p', 'mb-0', m.explanation));
                    card.appendChild(body);
                    list.appendChild(card);
                });
            })
            .catch(function (e) { clear(list); showError(errors, e.message); });
    }

    if (page === 'draft') initDraft();
    else if (page === 'candidates') initCandidates();
})();
