(function ($) {
    'use strict';
    var root = document.querySelector('.book-detail'); if (!root || !$) return;
    var qty = root.querySelector('.detail-qty'), input = qty.querySelector('input'), max = Number(qty.dataset.max);
    function clamp() { if (max <= 0) return; var value = parseInt(input.value, 10); input.value = Math.min(max, Math.max(1, isNaN(value) ? 1 : value)); }
    qty.addEventListener('click', function (event) { var button = event.target.closest('[data-qty]'); if (!button || max <= 0) return; clamp(); input.value = Number(input.value) + Number(button.dataset.qty); clamp(); });
    input.addEventListener('change', clamp); input.addEventListener('blur', clamp);
    var description = document.getElementById('bookDescription'), toggle = root.querySelector('[data-description-toggle]');
    if (description.scrollHeight > description.clientHeight + 2) toggle.classList.remove('d-none');
    toggle.addEventListener('click', function () { var collapsed = description.classList.toggle('is-collapsed'); toggle.textContent = collapsed ? 'Xem thêm' : 'Thu gọn'; toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true'); });
    root.querySelector('[data-share]').addEventListener('click', function () {
        var status = root.querySelector('[data-share-status]');
        if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(location.href).then(function () { status.textContent = 'Đã sao chép đường dẫn.'; }, function () { status.textContent = 'Không thể sao chép đường dẫn.'; });
        else { var field = document.createElement('textarea'); field.value = location.href; document.body.appendChild(field); field.select(); try { status.textContent = document.execCommand('copy') ? 'Đã sao chép đường dẫn.' : 'Không thể sao chép đường dẫn.'; } catch (e) { status.textContent = 'Không thể sao chép đường dẫn.'; } field.remove(); }
    });
    var reviewHost = document.getElementById('review-content'), reviewRequest;
    function loadReviews(url) { if (reviewRequest) reviewRequest.abort(); reviewHost.setAttribute('aria-busy', 'true'); reviewRequest = $.get(url).done(function (html) { reviewHost.innerHTML = html; }).fail(function (xhr, state) { if (state !== 'abort') { var notice = document.createElement('p'); notice.className = 'text-danger'; notice.textContent = 'Không thể tải đánh giá. Vui lòng thử lại.'; reviewHost.appendChild(notice); } }).always(function () { reviewHost.removeAttribute('aria-busy'); }); }
    reviewHost.addEventListener('submit', function (event) { var form = event.target.closest('.review-filter'); if (!form) return; event.preventDefault(); loadReviews(form.action + '?' + new URLSearchParams(new FormData(form)).toString()); });
    reviewHost.addEventListener('click', function (event) { var link = event.target.closest('.review-pages a'); if (link && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) { event.preventDefault(); loadReviews(link.href); } });
    var key = 'bookstore-recent-books', recentSection = document.getElementById('recent-section'), recentRequest;
    try {
        var recent = JSON.parse(localStorage.getItem(key) || '[]'); if (!Array.isArray(recent)) recent = [];
        var current = root.dataset.bookId; recent = [current].concat(recent.map(String).filter(function (id) { return /^\d+$/.test(id) && id !== current; }));
        recent = recent.filter(function (id, i) { return recent.indexOf(id) === i; }).slice(0, 10); localStorage.setItem(key, JSON.stringify(recent));
        var others = recent.filter(function (id) { return id !== current; });
        if (others.length) recentRequest = $.get(root.dataset.recentUrl, { ids: others.join(',') }).done(function (html) { var host = document.getElementById('recent-content'); host.innerHTML = html; if (host.querySelector('.book-card')) recentSection.classList.remove('d-none'); });
    } catch (e) { }
    root.querySelector('[data-clear-recent]').addEventListener('click', function () { if (recentRequest) recentRequest.abort(); try { localStorage.removeItem(key); } catch (e) { } recentSection.classList.add('d-none'); });
    var modal = document.getElementById('detailQuickModal'), modalBody = document.getElementById('detailQuickBody'), quickRequest, trigger, scroll;
    root.addEventListener('click', function (event) {
        var button = event.target.closest('[data-quick-book]'); if (!button || !window.bootstrap) return;
        trigger = button; scroll = { x: window.scrollX, y: window.scrollY }; if (quickRequest) quickRequest.abort();
        modalBody.textContent = 'Đang tải thông tin sách...'; bootstrap.Modal.getOrCreateInstance(modal).show(button);
        quickRequest = $.get(root.dataset.quickUrl, { id: button.dataset.quickBook }).done(function (html) { modalBody.innerHTML = html; }).fail(function (xhr, state) { if (state !== 'abort') modalBody.textContent = xhr.status === 404 ? 'Sách không còn tồn tại hoặc không được bán.' : 'Không thể tải thông tin sách.'; });
    });
    modal.addEventListener('hidden.bs.modal', function () { if (quickRequest) quickRequest.abort(); if (trigger && document.contains(trigger)) { try { trigger.focus({ preventScroll: true }); } catch (e) { } } if (scroll) window.scrollTo({ left: scroll.x, top: scroll.y, behavior: 'instant' }); });
    document.addEventListener('error', function (event) { var img = event.target; if (img.matches && img.matches('img[data-fallback]') && !img.dataset.fallbackUsed) { img.dataset.fallbackUsed = 'true'; img.src = img.dataset.fallback; } }, true);
    document.querySelectorAll('img[data-fallback]').forEach(function (img) { if (img.complete && img.naturalWidth === 0 && !img.dataset.fallbackUsed) { img.dataset.fallbackUsed = 'true'; img.src = img.dataset.fallback; } });
})(window.jQuery);
