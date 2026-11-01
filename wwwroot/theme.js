(function () {
    var root = document.documentElement;

    function savedTheme() {
        try {
            var t = localStorage.getItem('theme');
            if (t === 'dark' || t === 'light') { return t; }
        } catch (e) { }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function apply(theme) {
        if (root.getAttribute('data-bs-theme') !== theme) {
            root.setAttribute('data-bs-theme', theme);
        }
    }

    apply(savedTheme());

    // Blazor navigation can reset attributes on <html>; always restore the saved choice.
    new MutationObserver(function () { apply(savedTheme()); })
        .observe(root, { attributes: true, attributeFilter: ['data-bs-theme'] });

    document.addEventListener('click', function (e) {
        var btn = e.target.closest && e.target.closest('[data-theme-toggle]');
        if (!btn) { return; }
        var next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        try { localStorage.setItem('theme', next); } catch (e2) { }
        apply(next);
    });

    document.addEventListener('enhancedload', function () { apply(savedTheme()); });
})();
