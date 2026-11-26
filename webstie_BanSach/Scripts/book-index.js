(function ($) {
    'use strict';
    if (!$) return;
    var region = document.getElementById('book-list-region');
    var error = document.getElementById('book-load-error');
    var modalElement = document.getElementById('quickViewModal');
    var modalBody = document.getElementById('quickViewBody');
    var request, quickRequest, sequence = 0, previousHtml, retryUrl, retryPush, retryScroll;
    var quickScroll = null, quickTrigger = null;
    var mode = 'grid';
    try { mode = localStorage.getItem('book-view-mode') === 'list' ? 'list' : 'grid'; } catch (e) { }

    function applyMode() {
        var page = region.querySelector('.book-list-page');
        if (!page) return;
        page.classList.add('book-list-ready');
        var grid = page.querySelector('.book-grid');
        if (grid) grid.classList.toggle('is-list', mode === 'list');
        page.querySelectorAll('[data-view-mode]').forEach(function (button) {
            var selected = button.getAttribute('data-view-mode') === mode;
            button.classList.toggle('active', selected);
            button.setAttribute('aria-pressed', selected ? 'true' : 'false');
        });
    }

    function load(url, push, scrollResults) {
        retryUrl = url; retryPush = push; retryScroll = scrollResults;
        var current = ++sequence;
        if (request) request.abort(); else previousHtml = region.innerHTML;
        error.classList.add('d-none');
        var grid = region.querySelector('.book-grid');
        if (!grid) {
            var page = region.querySelector('.book-list-page');
            if (page) { grid = document.createElement('div'); grid.className = 'book-grid'; page.appendChild(grid); }
        }
        if (grid) {
            grid.classList.toggle('is-list', mode === 'list');
            grid.innerHTML = Array(9).join('<div class="book-skeleton" aria-hidden="true"></div>');
            grid.setAttribute('aria-busy', 'true');
        }
        request = $.ajax({ url: url, method: 'GET', cache: false });
        request.done(function (html) {
            if (current !== sequence) return;
            region.innerHTML = html;
            applyMode();
            var banner = region.querySelector('.category-banner');
            if (banner) banner.classList.add('category-banner--enter');
            if (push) history.pushState({}, '', url);
            if (scrollResults) {
                var results = region.querySelector('#book-results');
                if (results) results.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth', block: 'start' });
            }
            previousHtml = null;
        }).fail(function (xhr, status) {
            if (current !== sequence || status === 'abort') return;
            if (previousHtml != null) region.innerHTML = previousHtml;
            applyMode();
            error.classList.remove('d-none');
        }).always(function () { if (current === sequence) request = null; });
    }

    region.addEventListener('click', function (event) {
        var modeButton = event.target.closest('[data-view-mode]');
        if (modeButton) {
            mode = modeButton.getAttribute('data-view-mode');
            try { localStorage.setItem('book-view-mode', mode); } catch (e) { }
            applyMode(); return;
        }
        var quick = event.target.closest('[data-quick-book]');
        if (quick && window.bootstrap && bootstrap.Modal) {
            var id = quick.getAttribute('data-quick-book');
            quickScroll = { x: window.scrollX, y: window.scrollY };
            quickTrigger = quick;
            if (quickRequest) quickRequest.abort();
            modalBody.innerHTML = '<div class="text-center py-4" role="status"><span class="spinner-border text-primary" aria-hidden="true"></span><p class="mt-2 mb-0">Đang tải thông tin sách...</p></div>';
            bootstrap.Modal.getOrCreateInstance(modalElement).show(quick);
            quickRequest = $.ajax({ url: modalElement.getAttribute('data-quick-url'), data: { id: id }, method: 'GET', cache: false });
            quickRequest.done(function (html) { modalBody.innerHTML = html; });
            quickRequest.fail(function (xhr, status) { if (status !== 'abort') modalBody.textContent = xhr.status === 404 ? 'Sách không còn tồn tại hoặc không được bán.' : 'Không thể tải thông tin sách. Vui lòng thử lại.'; });
            return;
        }
        var link = event.target.closest('.book-categories a, .pager a');
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault(); load(link.href, true, !!link.closest('.pager'));
    });
    region.addEventListener('submit', function (event) {
        var form = event.target.closest('.pager-jump');
        if (!form) return;
        event.preventDefault();
        var params = new URLSearchParams(new FormData(form));
        load(form.action + '?' + params.toString(), true, true);
    });
    document.getElementById('book-load-retry').addEventListener('click', function () { if (retryUrl) load(retryUrl, retryPush, retryScroll); });
    window.addEventListener('popstate', function () { load(window.location.href, false); });
    modalElement.addEventListener('hidden.bs.modal', function () {
        if (quickRequest) { quickRequest.abort(); quickRequest = null; }
        if (quickTrigger && document.contains(quickTrigger)) {
            try { quickTrigger.focus({ preventScroll: true }); } catch (e) { quickTrigger.focus(); }
        }
        if (quickScroll) window.scrollTo({ left: quickScroll.x, top: quickScroll.y, behavior: 'instant' });
        quickScroll = null; quickTrigger = null;
    });
    document.addEventListener('error', function (event) {
        var image = event.target;
        if (image.matches && image.matches('img[data-fallback]') && !image.dataset.fallbackUsed) {
            image.dataset.fallbackUsed = 'true'; image.src = image.dataset.fallback;
        }
    }, true);
    document.querySelectorAll('img[data-fallback]').forEach(function (image) {
        if (image.complete && image.naturalWidth === 0 && !image.dataset.fallbackUsed) {
            image.dataset.fallbackUsed = 'true'; image.src = image.dataset.fallback;
        }
    });
    applyMode();
})(window.jQuery);
