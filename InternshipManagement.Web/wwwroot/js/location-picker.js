/* Location picker: adds a "Pick on map" button to any input marked data-location-picker, and draws a read-only
   map for any element marked data-location-map="<place name>".
   Map: Leaflet + OpenStreetMap tiles. Search and reverse lookup: OpenStreetMap Nominatim (free, no key).
   Without the map (offline, blocked, or the lookup fails) the input is still an ordinary text box. */
(function () {
    'use strict';

    var LEAFLET_CSS = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.css';
    var LEAFLET_JS = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.js';
    var NOMINATIM = 'https://nominatim.openstreetmap.org/';
    var TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
    var ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

    var leafletPromise = null;
    var nextSlot = 0;

    function injectStyles() {
        if (document.getElementById('lp-styles')) return;
        var css =
            '.lp-wrap{position:relative}' +
            '.lp-btn{margin-top:.35rem}' +
            '.lp-panel{display:none;margin-top:.5rem;padding:.6rem;border:1px solid #dee2e6;border-radius:.6rem;background:#fff;box-shadow:0 6px 20px rgba(15,23,42,.1)}' +
            '.lp-panel.lp-open{display:block}' +
            '.lp-pop{position:absolute;z-index:1050;left:0;width:min(420px,90vw)}' +
            '.lp-map{height:260px;border-radius:.4rem;margin-top:.5rem}' +
            '.lp-results{list-style:none;margin:.4rem 0 0;padding:0;max-height:140px;overflow:auto}' +
            '.lp-results button{width:100%;text-align:left;border:0;background:none;padding:.3rem .5rem;border-radius:.3rem;font-size:.85rem}' +
            '.lp-results button:hover{background:#f1f5f9}' +
            '.lp-status{font-size:.8rem;color:#64748b;margin-top:.35rem;min-height:1.1em}' +
            '.lp-view{height:200px;border-radius:.5rem;overflow:hidden}';
        var style = document.createElement('style');
        style.id = 'lp-styles';
        style.textContent = css;
        document.head.appendChild(style);
    }

    function loadLeaflet() {
        if (window.L) return Promise.resolve();
        if (leafletPromise) return leafletPromise;

        leafletPromise = new Promise(function (resolve, reject) {
            var link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = LEAFLET_CSS;
            document.head.appendChild(link);

            var script = document.createElement('script');
            script.src = LEAFLET_JS;
            script.onload = resolve;
            script.onerror = function () { leafletPromise = null; reject(new Error('The map could not be loaded.')); };
            document.head.appendChild(script);
        });
        return leafletPromise;
    }

    // Nominatim asks for at most one request per second, so lookups are queued about 1.1 s apart.
    function lookup(path) {
        var now = Date.now();
        var wait = Math.max(0, nextSlot - now);
        nextSlot = now + wait + 1100;

        return new Promise(function (resolve) { setTimeout(resolve, wait); })
            .then(function () {
                return fetch(NOMINATIM + path, { headers: { 'Accept': 'application/json', 'Accept-Language': 'en' } });
            })
            .then(function (r) { if (!r.ok) throw new Error('lookup failed'); return r.json(); });
    }

    function townOf(a) {
        a = a || {};
        return a.city || a.town || a.village || a.municipality || a.suburb || a.county || a.state || '';
    }

    function place(address, displayName, mode) {
        var a = address || {};
        var town = townOf(a);
        if (mode === 'address') return displayName || [town, a.country].filter(Boolean).join(', ');
        if (mode === 'city') return town || a.country || displayName || '';
        var parts = [town, a.country].filter(Boolean);
        return parts.length ? parts.join(', ') : (displayName || '');
    }

    function createMap(el, options) {
        var map = L.map(el, options);
        L.tileLayer(TILES, { maxZoom: 18, attribution: ATTRIBUTION }).addTo(map);
        return map;
    }

    // ---------------------------------------------------------------- picker
    function enhancePicker(input) {
        if (input.dataset.lpReady) return;
        input.dataset.lpReady = '1';

        var mode = input.dataset.locationMode || 'full';
        var popover = input.hasAttribute('data-location-popover');
        var zoom = mode === 'address' ? 18 : 10;
        // optional: also fill separate City and Country boxes (CSS selectors in data-location-city / -country)
        var cityBox = input.dataset.locationCity ? document.querySelector(input.dataset.locationCity) : null;
        var countryBox = input.dataset.locationCountry ? document.querySelector(input.dataset.locationCountry) : null;

        function setBox(box, value) {
            if (!box || !value) return;
            box.value = value;
            box.dispatchEvent(new Event('input', { bubbles: true }));
            box.dispatchEvent(new Event('change', { bubbles: true }));
        }

        var wrap = document.createElement('div');
        wrap.className = 'lp-wrap';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);

        var open = document.createElement('button');
        open.type = 'button';
        open.className = 'btn btn-outline-secondary btn-sm lp-btn';
        open.innerHTML = '<i class="fas fa-map-marker-alt me-1"></i> Pick on map';
        wrap.appendChild(open);

        var panel = document.createElement('div');
        panel.className = 'lp-panel' + (popover ? ' lp-pop' : '');
        panel.innerHTML =
            '<div class="input-group input-group-sm">' +
            '<input type="text" class="form-control lp-q" placeholder="Search for a city or address" aria-label="Search for a place">' +
            '<button type="button" class="btn btn-outline-primary lp-go">Search</button>' +
            '<button type="button" class="btn btn-outline-secondary lp-close" aria-label="Close map">&times;</button>' +
            '</div>' +
            '<ul class="lp-results"></ul>' +
            '<div class="lp-map"></div>' +
            '<div class="lp-status">Click the map to choose a place.</div>';
        wrap.appendChild(panel);

        var q = panel.querySelector('.lp-q');
        var go = panel.querySelector('.lp-go');
        var results = panel.querySelector('.lp-results');
        var status = panel.querySelector('.lp-status');
        var mapEl = panel.querySelector('.lp-map');
        var map = null, marker = null;

        function say(text) { status.textContent = text; }

        function choose(lat, lon, text, address) {
            if (marker) marker.setLatLng([lat, lon]); else marker = L.marker([lat, lon]).addTo(map);
            input.value = text;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));
            setBox(cityBox, townOf(address));
            setBox(countryBox, (address || {}).country);
            say('Selected: ' + text);
        }

        function search() {
            var term = q.value.trim();
            if (term.length < 2) { say('Type at least 2 characters.'); return; }
            say('Searching...');
            results.innerHTML = '';

            lookup('search?format=jsonv2&addressdetails=1&limit=5&q=' + encodeURIComponent(term))
                .then(function (items) {
                    if (!items.length) { say('Nothing found. Try another spelling or click the map.'); return; }
                    say('Choose a result.');
                    items.forEach(function (item) {
                        var li = document.createElement('li');
                        var b = document.createElement('button');
                        b.type = 'button';
                        b.textContent = item.display_name;
                        b.addEventListener('click', function () {
                            var lat = parseFloat(item.lat), lon = parseFloat(item.lon);
                            map.setView([lat, lon], 11);
                            choose(lat, lon, place(item.address, item.display_name, mode), item.address);
                            results.innerHTML = '';
                        });
                        li.appendChild(b);
                        results.appendChild(li);
                    });
                })
                .catch(function () { say('Search is not available right now. You can still type the place in the box.'); });
        }

        function init() {
            return loadLeaflet().then(function () {
                if (map) { map.invalidateSize(); return; }
                map = createMap(mapEl, { center: [0, 20], zoom: 2, scrollWheelZoom: true });

                map.on('click', function (e) {
                    say('Looking up that place...');
                    lookup('reverse?format=jsonv2&addressdetails=1&zoom=' + zoom + '&lat=' + e.latlng.lat + '&lon=' + e.latlng.lng)
                        .then(function (r) {
                            var text = place(r.address, r.display_name, mode);
                            if (!text) { say('No place found there. Try a nearby spot.'); return; }
                            choose(e.latlng.lat, e.latlng.lng, text, r.address);
                        })
                        .catch(function () { say('Could not look that place up. Try again, or type it in the box.'); });
                });

                // start where the box already points, if it has something in it
                var current = input.value.trim();
                if (current) {
                    lookup('search?format=jsonv2&limit=1&q=' + encodeURIComponent(current))
                        .then(function (items) {
                            if (items.length) {
                                var lat = parseFloat(items[0].lat), lon = parseFloat(items[0].lon);
                                map.setView([lat, lon], 10);
                                marker = L.marker([lat, lon]).addTo(map);
                            }
                        })
                        .catch(function () { /* the world view is fine */ });
                }
            });
        }

        open.addEventListener('click', function () {
            var show = !panel.classList.contains('lp-open');
            panel.classList.toggle('lp-open', show);
            if (!show) return;
            say('Loading the map...');
            init().then(function () { say('Click the map to choose a place.'); })
                  .catch(function (e) { say(e.message + ' You can still type the place in the box.'); });
        });
        panel.querySelector('.lp-close').addEventListener('click', function () { panel.classList.remove('lp-open'); });
        go.addEventListener('click', search);
        q.addEventListener('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); search(); } });
    }

    // ------------------------------------------------------------ read-only map
    function enhanceView(el) {
        if (el.dataset.lpReady) return;
        el.dataset.lpReady = '1';

        var name = (el.getAttribute('data-location-map') || '').trim();
        if (!name) { el.style.display = 'none'; return; }
        el.classList.add('lp-view');

        Promise.all([loadLeaflet(), lookup('search?format=jsonv2&limit=1&q=' + encodeURIComponent(name))])
            .then(function (r) {
                var items = r[1];
                if (!items.length) throw new Error('not found');
                var lat = parseFloat(items[0].lat), lon = parseFloat(items[0].lon);
                var map = createMap(el, { center: [lat, lon], zoom: 11, scrollWheelZoom: false });
                L.marker([lat, lon]).addTo(map).bindPopup(name.replace(/</g, '&lt;'));
            })
            .catch(function () { el.style.display = 'none'; });
    }

    function run() {
        var pickers = document.querySelectorAll('input[data-location-picker]');
        var views = document.querySelectorAll('[data-location-map]');
        if (pickers.length || views.length) injectStyles();
        Array.prototype.forEach.call(pickers, enhancePicker);
        Array.prototype.forEach.call(views, enhanceView);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', run); else run();
})();
