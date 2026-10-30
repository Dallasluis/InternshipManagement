/* Student AI pages: matches, CV analysis, skill gaps, career assistant.
 * Everything the AI wrote is inserted with textContent (never innerHTML), so it cannot inject markup. */
(function () {
    'use strict';

    var root = document.getElementById('ai-root');
    if (!root) return;

    var tokenInput = root.querySelector('input[name="__RequestVerificationToken"]');
    var csrf = tokenInput ? tokenInput.value : '';
    var page = root.dataset.page;

    // ------------------------------------------------------------------ helpers

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function clear(node) { while (node.firstChild) node.removeChild(node.firstChild); }
    function sleep(ms) { return new Promise(function (r) { setTimeout(r, ms); }); }

    // The API may answer in PascalCase or camelCase depending on the path: normalise to camelCase.
    function camelize(value) {
        if (Array.isArray(value)) return value.map(camelize);
        if (value && typeof value === 'object') {
            var out = {};
            Object.keys(value).forEach(function (k) {
                out[k.charAt(0).toLowerCase() + k.slice(1)] = camelize(value[k]);
            });
            return out;
        }
        return value;
    }

    function friendlyStatus(status, data) {
        if (data && data.error) return data.error;
        if (status === 401) return 'Your session has expired. Please log in again.';
        if (status === 429) return 'You have used all your AI requests for now. Please try again in a while.';
        if (status === 503) return 'The AI service is busy or unavailable. Please try again in a minute.';
        if (status === 409) return 'This is already running. Please wait for it to finish.';
        return 'Something went wrong. Please try again.';
    }

    async function api(url, options) {
        options = options || {};
        var headers = options.headers || {};
        if (options.method && options.method !== 'GET') headers['RequestVerificationToken'] = csrf;
        options.headers = headers;
        options.credentials = 'same-origin';

        var response;
        try {
            response = await fetch(url, options);
        } catch (e) {
            throw new Error('Could not reach the server. Check your connection and try again.');
        }

        var data = null;
        try { data = camelize(await response.json()); } catch (e) { data = null; }

        if (response.status === 401) {
            window.location.href = root.dataset.loginUrl;
            throw new Error('Your session has expired. Please log in again.');
        }
        if (!response.ok) throw new Error(friendlyStatus(response.status, data));
        return data;
    }

    // Starts a background job and waits until it is Done (or Failed). Returns the job's result.
    async function runJob(start, onStatus) {
        var job = await start();
        if (!job || !job.id) throw new Error('The request could not be started. Please try again.');

        var deadline = Date.now() + 5 * 60 * 1000;
        var delay = 1500;
        while (true) {
            if (Date.now() > deadline) {
                throw new Error('This is taking longer than expected. Please try again in a moment.');
            }
            await sleep(delay);
            delay = Math.min(Math.round(delay * 1.3), 4000);

            var current = await api(root.dataset.jobUrl + '?id=' + encodeURIComponent(job.id));
            if (onStatus) onStatus(current.status);
            if (current.status === 'Done') return current.result;
            if (current.status === 'Failed') throw new Error(current.error || 'The analysis failed. Please try again.');
        }
    }

    function setBusy(button, busy, busyText, idleText) {
        button.disabled = busy;
        button.textContent = busy ? busyText : idleText;
    }

    function showError(container, message) {
        clear(container);
        if (!message) return;
        var box = el('div', 'alert alert-danger mb-3', message);
        box.setAttribute('role', 'alert');
        container.appendChild(box);
    }

    function badges(list, className) {
        var wrap = el('div', 'd-flex flex-wrap gap-1 mb-2');
        (list || []).forEach(function (item) { wrap.appendChild(el('span', 'badge ' + className, item)); });
        return wrap;
    }

    // ------------------------------------------------------------------ matches page

    function scoreClass(score) {
        if (score >= 75) return 'bg-success';
        if (score >= 50) return 'bg-warning text-dark';
        return 'bg-secondary';
    }

    function renderMatches(container, matches, showAi) {
        clear(container);
        if (!matches || !matches.length) {
            container.appendChild(el('div', 'text-muted py-4 text-center',
                'No matching internships yet. Add skills and qualifications to your profile, or check back when new internships are published.'));
            return;
        }
        matches.forEach(function (m) {
            var card = el('div', 'card mb-3 ai-card');
            var body = el('div', 'card-body');

            var head = el('div', 'd-flex justify-content-between align-items-start mb-2');
            var titleBox = el('div');
            var link = el('a', 'h5 text-decoration-none', m.title);
            link.href = root.dataset.detailsUrl + '/' + encodeURIComponent(m.targetId);
            titleBox.appendChild(link);
            if (m.subtitle) titleBox.appendChild(el('div', 'text-muted', m.subtitle));
            head.appendChild(titleBox);
            head.appendChild(el('span', 'badge fs-6 ' + scoreClass(m.matchScore), m.matchScore + '% match'));
            body.appendChild(head);

            if (m.matchingSkills && m.matchingSkills.length) {
                body.appendChild(el('div', 'small text-muted mb-1', 'Skills you have'));
                body.appendChild(badges(m.matchingSkills, 'bg-success-subtle text-success-emphasis border'));
            }
            if (m.missingPreferredSkills && m.missingPreferredSkills.length) {
                body.appendChild(el('div', 'small text-muted mb-1', 'Nice to have'));
                body.appendChild(badges(m.missingPreferredSkills, 'bg-light text-dark border'));
            }

            if (m.explanation) body.appendChild(el('p', 'mb-2', m.explanation));

            if (showAi && m.aiExplanation) {
                var ai = el('div', 'ai-note');
                ai.appendChild(el('div', 'ai-note-label', 'AI explanation'));
                ai.appendChild(el('div', null, m.aiExplanation));
                body.appendChild(ai);
            }

            card.appendChild(body);
            container.appendChild(card);
        });
    }

    function initMatches() {
        var list = document.getElementById('matches-list');
        var errors = document.getElementById('matches-errors');
        var button = document.getElementById('explain-btn');
        var count = document.getElementById('match-count');
        var note = document.getElementById('matches-note');

        api(root.dataset.quickUrl + '?maxResults=10')
            .then(function (data) { renderMatches(list, data, false); })
            .catch(function (e) { clear(list); showError(errors, e.message); });

        button.addEventListener('click', async function () {
            showError(errors, '');
            note.textContent = '';
            setBusy(button, true, 'Working on it…', 'Explain my matches with AI');
            try {
                var result = await runJob(function () {
                    return api(root.dataset.startUrl + '?maxResults=' + encodeURIComponent(count.value), { method: 'POST' });
                }, function (status) {
                    note.textContent = status === 'Running' ? 'The AI is writing your explanations…' : 'Waiting in line…';
                });
                renderMatches(list, result.matches, !!result.aiExplanationsAvailable);
                note.textContent = result.aiExplanationsAvailable
                    ? 'AI explanations are written from your profile and the internship details. Check them before you decide.'
                    : 'AI explanations are not available right now, so the standard match details are shown.';
            } catch (e) {
                showError(errors, e.message);
                note.textContent = '';
            } finally {
                setBusy(button, false, '', 'Explain my matches with AI');
            }
        });
    }

    // ------------------------------------------------------------------ skill gaps page

    function renderGaps(container, data) {
        clear(container);

        if (!data.targets || !data.targets.length) {
            container.appendChild(el('div', 'text-muted py-4 text-center',
                'There are no open internships to compare with your profile yet.'));
            return;
        }

        if (data.aiSummary) {
            var ai = el('div', 'ai-note mb-4');
            ai.appendChild(el('div', 'ai-note-label', 'AI summary'));
            ai.appendChild(el('div', null, data.aiSummary));
            container.appendChild(ai);
        }

        container.appendChild(el('h2', 'h5 mb-2', 'Compared with ' + data.targets.length + ' internship' + (data.targets.length === 1 ? '' : 's')));
        var targets = el('ul', 'list-unstyled small text-muted mb-4');
        data.targets.forEach(function (t) {
            var li = el('li');
            var a = el('a', 'text-decoration-none', t.title + (t.company ? ' at ' + t.company : ''));
            a.href = root.dataset.detailsUrl + '/' + encodeURIComponent(t.internshipId);
            li.appendChild(a);
            if (t.matchScore != null) li.appendChild(document.createTextNode(' (' + t.matchScore + '% match)'));
            targets.appendChild(li);
        });
        container.appendChild(targets);

        container.appendChild(el('h2', 'h5 mb-2', 'What is missing'));
        if (!data.gaps || !data.gaps.length) {
            container.appendChild(el('p', 'text-success mb-4', 'No gaps found. Your profile covers what these internships ask for.'));
        } else {
            var wrap = el('div', 'table-responsive mb-4');
            var table = el('table', 'table table-sm align-middle');
            var thead = el('thead');
            var hr = el('tr');
            ['Skill or qualification', 'Type', 'Importance', 'Asked by'].forEach(function (h) { hr.appendChild(el('th', null, h)); });
            thead.appendChild(hr);
            table.appendChild(thead);
            var tbody = el('tbody');
            data.gaps.forEach(function (g) {
                var tr = el('tr');
                tr.appendChild(el('td', 'fw-semibold', g.term));
                tr.appendChild(el('td', null, g.kind));
                var imp = el('td');
                imp.appendChild(el('span', 'badge ' + (g.importance === 'Required' ? 'bg-danger-subtle text-danger-emphasis border' : 'bg-light text-dark border'), g.importance));
                tr.appendChild(imp);
                tr.appendChild(el('td', null, g.internshipCount + ' internship' + (g.internshipCount === 1 ? '' : 's')));
                tbody.appendChild(tr);
            });
            table.appendChild(tbody);
            wrap.appendChild(table);
            container.appendChild(wrap);
        }

        if (data.recommendations && data.recommendations.length) {
            container.appendChild(el('h2', 'h5 mb-2', 'Certificates that could help'));
            var grid = el('div', 'row g-3 mb-3');
            data.recommendations.forEach(function (r) {
                var col = el('div', 'col-md-6');
                var card = el('div', 'card h-100');
                var body = el('div', 'card-body');
                body.appendChild(el('div', 'fw-semibold', r.name));
                body.appendChild(el('div', 'small text-muted mb-2', r.provider + ' · ' + r.level));
                body.appendChild(badges(r.covers, 'bg-light text-dark border'));
                if (r.why) body.appendChild(el('div', 'small', r.why));
                card.appendChild(body);
                col.appendChild(card);
                grid.appendChild(col);
            });
            container.appendChild(grid);
        }

        (data.notes || []).forEach(function (n) { container.appendChild(el('p', 'small text-muted mb-1', n)); });
    }

    function initGaps() {
        var out = document.getElementById('gaps-result');
        var errors = document.getElementById('gaps-errors');
        var button = document.getElementById('gaps-ai-btn');
        var note = document.getElementById('gaps-note');
        var internshipId = root.dataset.internshipId || '';
        var query = internshipId ? '?internshipId=' + encodeURIComponent(internshipId) : '';

        api(root.dataset.gapsUrl + query)
            .then(function (data) { renderGaps(out, data); })
            .catch(function (e) { clear(out); showError(errors, e.message); });

        button.addEventListener('click', async function () {
            showError(errors, '');
            setBusy(button, true, 'Working on it…', 'Add an AI summary');
            try {
                var result = await runJob(function () {
                    return api(root.dataset.startUrl + query, { method: 'POST' });
                }, function (status) {
                    note.textContent = status === 'Running' ? 'The AI is writing your summary…' : 'Waiting in line…';
                });
                renderGaps(out, result);
                note.textContent = result.aiAvailable ? '' : 'The AI summary is not available right now, so only the standard results are shown.';
            } catch (e) {
                showError(errors, e.message);
                note.textContent = '';
            } finally {
                setBusy(button, false, '', 'Add an AI summary');
            }
        });
    }

    // ------------------------------------------------------------------ CV analysis page

    var SECTIONS = [
        { key: 'skills', title: 'Skills', apiKey: 'skills',
          label: function (i) { return i.name + (i.proficiencyLevel ? ' (' + i.proficiencyLevel + ')' : ''); },
          build: function (i) { return { name: i.name, proficiencyLevel: i.proficiencyLevel || null }; } },
        { key: 'education', title: 'Education', apiKey: 'education', dates: true,
          label: function (i) { return [i.degree, i.fieldOfStudy].filter(Boolean).join(' in ') + ' at ' + i.institution; },
          build: function (i, start) { return { institution: i.institution, degree: i.degree, fieldOfStudy: i.fieldOfStudy, startDate: start, endDate: i.endDate || null, isCurrent: !!i.isCurrent, grade: i.grade || null, completionStatus: i.completionStatus || null }; } },
        { key: 'workExperience', title: 'Work experience', apiKey: 'workExperience', dates: true,
          label: function (i) { return i.position + ' at ' + i.company; },
          build: function (i, start) { return { company: i.company, position: i.position, description: i.description || null, startDate: start, endDate: i.endDate || null, isCurrent: !!i.isCurrent }; } },
        { key: 'certifications', title: 'Certificates', apiKey: 'certifications',
          label: function (i) { return i.name + (i.issuingOrganization ? ' — ' + i.issuingOrganization : ''); },
          build: function (i) { return { name: i.name, issuingOrganization: i.issuingOrganization, issueDate: i.issueDate || null, expirationDate: i.expirationDate || null }; } },
        { key: 'projects', title: 'Projects', apiKey: 'projects',
          label: function (i) { return i.title + (i.technologies ? ' (' + i.technologies + ')' : ''); },
          build: function (i) { return { title: i.title, description: i.description || null, role: i.role || null, technologies: i.technologies || null }; } }
    ];

    function renderCvResult(container, data, onConfirm) {
        clear(container);
        var rows = []; // { section, item, checkbox, dateInput }

        container.appendChild(el('p', 'text-muted',
            'Tick what you want to add to your profile. Nothing is saved until you press "Add selected to my profile".'));

        SECTIONS.forEach(function (section) {
            var items = data[section.key] || [];
            if (!items.length) return;

            var card = el('div', 'card mb-3');
            var body = el('div', 'card-body');
            body.appendChild(el('h2', 'h6 mb-3', section.title));

            items.forEach(function (item) {
                var already = item.status === 'AlreadyOnProfile';
                var row = el('div', 'd-flex align-items-start gap-2 py-2 border-top');

                var cb = el('input', 'form-check-input mt-1');
                cb.type = 'checkbox';
                cb.checked = !already;
                cb.disabled = already;
                row.appendChild(cb);

                var text = el('div', 'flex-grow-1');
                text.appendChild(el('div', null, section.label(item)));
                if (already) text.appendChild(el('div', 'small text-muted', 'Already on your profile'));

                var dateInput = null;
                if (section.dates && !already && item.needsStartDate) {
                    var wrap = el('div', 'mt-1');
                    wrap.appendChild(el('label', 'small text-danger me-2', 'Start date needed:'));
                    dateInput = el('input', 'form-control form-control-sm d-inline-block w-auto');
                    dateInput.type = 'date';
                    dateInput.required = true;
                    wrap.appendChild(dateInput);
                    text.appendChild(wrap);
                }
                row.appendChild(text);
                body.appendChild(row);
                rows.push({ section: section, item: item, checkbox: cb, dateInput: dateInput });
            });

            card.appendChild(body);
            container.appendChild(card);
        });

        var nm = data.notMentionedInCv || {};
        var notFound = [];
        SECTIONS.forEach(function (s) { (nm[s.key] || []).forEach(function (n) { notFound.push(s.title + ': ' + n); }); });
        if (notFound.length) {
            var box = el('div', 'alert alert-light border small');
            box.appendChild(el('div', 'fw-semibold mb-1', 'Already on your profile but not found in this CV'));
            notFound.forEach(function (n) { box.appendChild(el('div', null, n)); });
            container.appendChild(box);
        }
        (data.notes || []).forEach(function (n) { container.appendChild(el('p', 'small text-muted mb-1', n)); });

        var errors = el('div');
        container.appendChild(errors);
        var confirm = el('button', 'btn btn-primary mt-2', 'Add selected to my profile');
        confirm.type = 'button';
        container.appendChild(confirm);

        confirm.addEventListener('click', function () {
            showError(errors, '');
            var payload = { skills: [], education: [], workExperience: [], certifications: [], projects: [] };
            var count = 0;

            for (var i = 0; i < rows.length; i++) {
                var r = rows[i];
                if (!r.checkbox.checked) continue;
                var start = r.item.startDate || null;
                if (r.dateInput) {
                    if (!r.dateInput.value) {
                        showError(errors, 'Enter a start date for "' + r.section.label(r.item) + '", or untick it.');
                        r.dateInput.focus();
                        return;
                    }
                    start = r.dateInput.value;
                }
                payload[r.section.apiKey].push(r.section.build(r.item, start));
                count++;
            }
            if (!count) { showError(errors, 'Tick at least one item to add.'); return; }
            onConfirm(payload, confirm, errors);
        });
    }

    function initCv() {
        var form = document.getElementById('cv-form');
        var fileInput = document.getElementById('cv-file');
        var button = document.getElementById('cv-btn');
        var errors = document.getElementById('cv-errors');
        var note = document.getElementById('cv-note');
        var result = document.getElementById('cv-result');

        form.addEventListener('submit', async function (ev) {
            ev.preventDefault();
            showError(errors, '');
            clear(result);

            var file = fileInput.files[0];
            if (!file) { showError(errors, 'Choose your CV as a PDF first.'); return; }
            if (!/\.pdf$/i.test(file.name)) { showError(errors, 'Only PDF files are supported.'); return; }
            if (file.size > 5 * 1024 * 1024) { showError(errors, 'The CV is larger than 5 MB.'); return; }

            setBusy(button, true, 'Reading your CV…', 'Analyse my CV');
            try {
                var data = await runJob(function () {
                    var body = new FormData();
                    body.append('file', file);
                    return api(root.dataset.startUrl, { method: 'POST', body: body });
                }, function (status) {
                    note.textContent = status === 'Running' ? 'The AI is reading your CV. This can take up to a minute…' : 'Waiting in line…';
                });
                note.textContent = '';
                renderCvResult(result, data, async function (payload, confirmButton, confirmErrors) {
                    setBusy(confirmButton, true, 'Saving…', 'Add selected to my profile');
                    try {
                        var done = await api(root.dataset.confirmUrl, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(payload)
                        });
                        clear(result);
                        var ok = el('div', 'alert alert-success');
                        var added = (done.skillsAdded || 0) + (done.educationAdded || 0) + (done.workExperienceAdded || 0)
                            + (done.certificationsAdded || 0) + (done.projectsAdded || 0);
                        ok.appendChild(el('div', 'fw-semibold', added + ' item' + (added === 1 ? '' : 's') + ' added to your profile.'));
                        if (done.skippedAsDuplicate) ok.appendChild(el('div', 'small', done.skippedAsDuplicate + ' skipped because they were already there.'));
                        var link = el('a', 'btn btn-sm btn-outline-success mt-2', 'View my profile');
                        link.href = root.dataset.profileUrl;
                        ok.appendChild(link);
                        result.appendChild(ok);
                    } catch (e) {
                        showError(confirmErrors, e.message);
                        setBusy(confirmButton, false, '', 'Add selected to my profile');
                    }
                });
            } catch (e) {
                note.textContent = '';
                showError(errors, e.message);
            } finally {
                setBusy(button, false, '', 'Analyse my CV');
            }
        });
    }

    // ------------------------------------------------------------------ career assistant page

    function initAssistant() {
        var log = document.getElementById('chat-log');
        var form = document.getElementById('chat-form');
        var input = document.getElementById('chat-input');
        var send = document.getElementById('chat-send');
        var stop = document.getElementById('chat-stop');
        var reset = document.getElementById('chat-reset');
        var counter = document.getElementById('chat-count');
        var history = [];
        var controller = null;

        function addBubble(role, text) {
            var row = el('div', 'chat-row ' + role);
            var bubble = el('div', 'chat-bubble', text);
            row.appendChild(bubble);
            log.appendChild(row);
            log.scrollTop = log.scrollHeight;
            return bubble;
        }

        function setSending(sending) {
            send.disabled = sending;
            input.disabled = sending;
            stop.classList.toggle('d-none', !sending);
            if (!sending) input.focus();
        }

        async function ask(message) {
            addBubble('user', message);
            var bubble = addBubble('assistant', '');
            bubble.classList.add('typing');
            var answer = '';
            var failed = false;
            setSending(true);
            controller = new AbortController();

            try {
                var response = await fetch(root.dataset.chatUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf },
                    body: JSON.stringify({ message: message, history: history.slice(-10) }),
                    signal: controller.signal
                });

                if (response.status === 401) { window.location.href = root.dataset.loginUrl; return; }
                if (!response.ok) {
                    var data = null;
                    try { data = camelize(await response.json()); } catch (e) { data = null; }
                    throw new Error(friendlyStatus(response.status, data));
                }

                var reader = response.body.getReader();
                var decoder = new TextDecoder();
                var buffer = '';
                while (true) {
                    var chunk = await reader.read();
                    if (chunk.done) break;
                    buffer += decoder.decode(chunk.value, { stream: true });

                    var parts = buffer.split('\n\n');
                    buffer = parts.pop();
                    for (var p = 0; p < parts.length; p++) {
                        var name = 'message';
                        var dataText = '';
                        parts[p].split('\n').forEach(function (line) {
                            if (line.indexOf('event:') === 0) name = line.slice(6).trim();
                            else if (line.indexOf('data:') === 0) dataText += line.slice(5).trim();
                        });
                        if (!dataText) continue;
                        var payload;
                        try { payload = JSON.parse(dataText); } catch (e) { continue; }

                        if (name === 'error') {
                            failed = true;
                            throw new Error(payload.message || 'The assistant is unavailable right now. Please try again.');
                        }
                        if (name === 'message' && typeof payload.text === 'string') {
                            answer += payload.text;
                            bubble.textContent = answer;
                            log.scrollTop = log.scrollHeight;
                        }
                    }
                }
            } catch (e) {
                if (e.name === 'AbortError') {
                    if (!answer) bubble.textContent = '(stopped)';
                } else {
                    failed = true;
                    bubble.classList.add('error');
                    bubble.textContent = answer ? answer + '\n\n' + e.message : e.message;
                }
            } finally {
                bubble.classList.remove('typing');
                controller = null;
                setSending(false);
            }

            if (answer && !failed) {
                history.push({ role: 'user', content: message });
                history.push({ role: 'assistant', content: answer });
            }
        }

        form.addEventListener('submit', function (ev) {
            ev.preventDefault();
            var message = input.value.trim();
            if (!message) return;
            input.value = '';
            counter.textContent = '0 / 1000';
            ask(message.slice(0, 1000));
        });

        input.addEventListener('keydown', function (ev) {
            if (ev.key === 'Enter' && !ev.shiftKey) { ev.preventDefault(); form.requestSubmit(); }
        });
        input.addEventListener('input', function () { counter.textContent = input.value.length + ' / 1000'; });
        stop.addEventListener('click', function () { if (controller) controller.abort(); });
        reset.addEventListener('click', function () {
            if (controller) controller.abort();
            history = [];
            clear(log);
            addBubble('assistant', root.dataset.greeting);
        });
        document.querySelectorAll('[data-prompt]').forEach(function (chip) {
            chip.addEventListener('click', function () {
                if (send.disabled) return;
                input.value = chip.dataset.prompt;
                form.requestSubmit();
            });
        });

        addBubble('assistant', root.dataset.greeting);
    }

    // ------------------------------------------------------------------ start

    if (page === 'matches') initMatches();
    else if (page === 'gaps') initGaps();
    else if (page === 'cv') initCv();
    else if (page === 'assistant') initAssistant();
})();
