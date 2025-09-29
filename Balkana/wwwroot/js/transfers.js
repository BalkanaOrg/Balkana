(function () {
    const bodyEl = document.getElementById('transfersBody');
    const sentinel = document.getElementById('infiniteSentinel');
    const totalEl = document.getElementById('totalTransfers');
    const pageSizeEl = document.getElementById('pageSize');
    const initialPageEl = document.getElementById('initialPage');

    const searchEl = document.getElementById('searchInput');
    const gameEl = document.getElementById('gameSelect');
    const asOfEl = document.getElementById('asOfDate');

    const total = parseInt(totalEl?.value || '0', 10);
    const pageSize = parseInt(pageSizeEl?.value || '40', 10);
    let currentPage = parseInt(initialPageEl?.value || '1', 10);
    let loading = false;
    let done = false;

    function getParams() {
        const params = new URLSearchParams();
        const search = (searchEl?.value || '').trim();
        const game = (gameEl?.value || '').trim();
        const asOf = (asOfEl?.value || '').trim();

        if (search) params.set('SearchTerm', search);
        if (game) params.set('Game', game);
        if (asOf) params.set('AsOfDate', asOf);
        params.set('CurrentPage', String(currentPage));
        return params;
    }

    async function loadPage(append = true) {
        if (loading || done) return;
        loading = true;
        try {
            const params = getParams();
            const url = `/Transfers/Load?${params.toString()}`;
            const res = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!res.ok) throw new Error(`Failed to load: ${res.status}`);
            const html = await res.text();

            const temp = document.createElement('tbody');
            temp.innerHTML = html;

            if (!append) {
                bodyEl.innerHTML = '';
            }
            while (temp.firstChild) {
                bodyEl.appendChild(temp.firstChild);
            }

            const loadedCount = bodyEl.querySelectorAll('tr').length;
            if (loadedCount >= total || html.trim().length === 0) {
                done = true;
            } else {
                currentPage += 1;
            }
        } catch (err) {
            console.error(err);
            done = true;
        } finally {
            loading = false;
        }
    }

    function debounce(fn, wait) {
        let t;
        return function (...args) {
            clearTimeout(t);
            t = setTimeout(() => fn.apply(this, args), wait);
        };
    }

    const onFilterChanged = debounce(() => {
        currentPage = 1;
        done = false;
        loadPage(false);
    }, 300);

    searchEl?.addEventListener('input', onFilterChanged);
    gameEl?.addEventListener('change', onFilterChanged);
    asOfEl?.addEventListener('change', onFilterChanged);

    const io = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                loadPage(true);
            }
        });
    }, { root: null, rootMargin: '0px 0px 200px 0px', threshold: 0 });

    if (sentinel) io.observe(sentinel);
})();