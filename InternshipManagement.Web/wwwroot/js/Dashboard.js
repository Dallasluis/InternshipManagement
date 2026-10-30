(function () {
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Numbers count up once.
    document.querySelectorAll('[data-count]').forEach(function (node) {
        var target = parseInt(node.getAttribute('data-count'), 10);
        if (isNaN(target) || reduce || target === 0) { node.textContent = isNaN(target) ? node.textContent : target.toLocaleString(); return; }
        var start = null, duration = 700;
        function step(ts) {
            if (start === null) start = ts;
            var t = Math.min(1, (ts - start) / duration);
            node.textContent = Math.round(target * (1 - Math.pow(1 - t, 3))).toLocaleString();
            if (t < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    });

    // Segmented bar widths (set after paint so they animate).
    requestAnimationFrame(function () {
        document.querySelectorAll('.pipe span[data-w]').forEach(function (s) { s.style.width = s.getAttribute('data-w') + '%'; });
        document.querySelectorAll('.ring[data-p]').forEach(function (r) { r.style.setProperty('--p', r.getAttribute('data-p')); });
    });

    // Staggered entrance.
    document.querySelectorAll('.dash-in').forEach(function (node, i) { node.style.animationDelay = (i * 60) + 'ms'; });

    // Time-aware greeting.
    var g = document.getElementById('dash-greeting');
    if (g) { var h = new Date().getHours(); g.textContent = h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'; }
    var d = document.getElementById('dash-date');
    if (d) d.textContent = new Date().toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' });
})();
