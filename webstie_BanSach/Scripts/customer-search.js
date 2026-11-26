(function ($) {
    'use strict';
    var form = document.querySelector('.customer-search');
    if (!form || !$) return;
    var input = form.querySelector('input[name=q]'), box = document.getElementById('search-suggestions');
    var modal = document.getElementById('searchQuickModal'), modalBody = document.getElementById('searchQuickBody');
    var timer, request, quickRequest, sequence = 0, active = -1;
    var key = 'bookstore-search-history';
    function clean(s) { return (s || '').trim().replace(/\s+/g, ' '); }
    function fold(s) { return s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase(); }
    function history() { try { var x = JSON.parse(localStorage.getItem(key) || '[]'); return Array.isArray(x) ? x.filter(function (v) { return typeof v === 'string'; }).slice(0, 5) : []; } catch (e) { return []; } }
    function save(items) { try { localStorage.setItem(key, JSON.stringify(items.slice(0, 5))); } catch (e) { } }
    function remember(value) { value = clean(value); if (!value) return; save([value].concat(history().filter(function (x) { return fold(x) !== fold(value); }))); }
    function hide() { ++sequence; clearTimeout(timer); if (request) { request.abort(); request = null; } box.hidden = true; input.setAttribute('aria-expanded', 'false'); input.removeAttribute('aria-activedescendant'); active = -1; }
    function show() { box.hidden = false; input.setAttribute('aria-expanded', 'true'); active = -1; }
    function text(tag, value, className) { var el = document.createElement(tag); if (className) el.className = className; el.textContent = value; return el; }
    function option(label, kind, value) { var b = document.createElement('button'); b.type = 'button'; b.className = 'search-option'; b.setAttribute('role', 'option'); b.dataset.kind = kind; b.dataset.value = value; b.appendChild(text('span', label)); return b; }
    function showHistory() {
        box.replaceChildren(); var items = history(); if (!items.length) { hide(); return; }
        var heading = text('div', 'Tìm kiếm gần đây', 'search-heading'); var clear = option('Xóa tất cả', 'clear', ''); clear.className = 'search-clear'; clear.removeAttribute('role'); heading.appendChild(clear); box.appendChild(heading);
        items.forEach(function (term) { var row = document.createElement('div'); row.className = 'search-history-row'; row.appendChild(option(term, 'history', term)); var remove = option('×', 'remove', term); remove.className = 'search-remove'; remove.removeAttribute('role'); remove.setAttribute('aria-label', 'Xóa ' + term); row.appendChild(remove); box.appendChild(row); }); show();
    }
    function highlight(container, title, term) {
        var foldedTitle = '', positions = [];
        Array.from(title).forEach(function (ch, index) { var part = fold(ch); for (var i = 0; i < part.length; i++) { foldedTitle += part[i]; positions.push(index); } });
        var index = foldedTitle.indexOf(fold(term)); var chars = Array.from(title);
        if (index < 0) { container.textContent = title; return; }
        var start = positions[index], end = positions[index + fold(term).length - 1] + 1;
        container.appendChild(document.createTextNode(chars.slice(0, start).join(''))); container.appendChild(text('mark', chars.slice(start, end).join(''))); container.appendChild(document.createTextNode(chars.slice(end).join('')));
    }
    function search() {
        var term = clean(input.value); ++sequence; clearTimeout(timer); if (request) request.abort();
        if (!term) { showHistory(); return; } if (term.length < 2) { hide(); return; }
        var current = sequence; box.replaceChildren(text('div', 'Đang tìm kiếm...', 'search-status')); show();
        timer = setTimeout(function () {
            request = $.ajax({ url: form.dataset.searchUrl, data: { q: term }, method: 'GET' });
            request.done(function (items) {
                if (current !== sequence) return; box.replaceChildren();
                if (!items.length) box.appendChild(text('div', 'Không tìm thấy sách phù hợp.', 'search-status'));
                items.forEach(function (item) {
                    var b = option('', 'book', String(item.id)); b.replaceChildren();
                    var img = document.createElement('img'); img.src = item.cover; img.alt = ''; img.width = 42; img.height = 58; img.dataset.fallback = form.dataset.fallback; b.appendChild(img);
                    var detail = document.createElement('span'); var title = text('strong', '', 'search-title'); highlight(title, item.title || '', term); detail.appendChild(title);
                    detail.appendChild(text('small', item.authors || 'Chưa có thông tin tác giả')); detail.appendChild(text('small', Number(item.price).toLocaleString('vi-VN') + ' ₫')); b.appendChild(detail); box.appendChild(b);
                });
                box.appendChild(option('Xem tất cả kết quả', 'all', term)); show();
            }).fail(function (xhr, status) { if (status !== 'abort' && current === sequence) { box.replaceChildren(text('div', 'Không thể tìm kiếm. Vui lòng thử lại.', 'search-status')); show(); } });
        }, 300);
    }
    function choose(item) {
        var kind = item.dataset.kind, value = item.dataset.value;
        if (kind === 'clear') { save([]); hide(); return; }
        if (kind === 'remove') { save(history().filter(function (x) { return x !== value; })); showHistory(); return; }
        if (kind === 'history') { input.value = value; remember(value); form.submit(); return; }
        if (kind === 'all') { input.value = value; remember(value); form.submit(); return; }
        if (kind === 'book' && window.bootstrap) {
            remember(input.value); hide(); if (quickRequest) quickRequest.abort();
            modalBody.textContent = 'Đang tải thông tin sách...'; bootstrap.Modal.getOrCreateInstance(modal).show(item);
            quickRequest = $.ajax({ url: form.dataset.quickUrl, data: { id: value }, method: 'GET' });
            quickRequest.done(function (html) { modalBody.innerHTML = html; });
            quickRequest.fail(function (xhr, status) { if (status !== 'abort') modalBody.textContent = xhr.status === 404 ? 'Sách không còn tồn tại hoặc không được bán.' : 'Không thể tải thông tin sách.'; });
        }
    }
    form.dataset.fallback = form.dataset.fallback || (window.location.pathname.replace(/\/(?:Home|Book)(?:\/.*)?$/i, '') + '/Content/Images/default-book.jpg');
    input.addEventListener('input', search);
    input.addEventListener('focus', function () { if (!clean(input.value)) showHistory(); else if (clean(input.value).length >= 2) search(); });
    input.addEventListener('keydown', function (e) {
        var items = Array.from(box.querySelectorAll('[role=option]'));
        if (e.key === 'Escape') { hide(); return; }
        if (!box.hidden && (e.key === 'ArrowDown' || e.key === 'ArrowUp') && items.length) {
            e.preventDefault(); active = active < 0 ? (e.key === 'ArrowDown' ? 0 : items.length - 1) : (active + (e.key === 'ArrowDown' ? 1 : -1) + items.length) % items.length;
            items.forEach(function (item, i) { item.classList.toggle('active', i === active); item.id = 'search-option-' + i; }); input.setAttribute('aria-activedescendant', items[active].id); items[active].scrollIntoView({ block: 'nearest' });
        } else if (e.key === 'Enter' && !box.hidden && active >= 0 && items[active]) { e.preventDefault(); choose(items[active]); }
    });
    box.addEventListener('click', function (e) { var item = e.target.closest('[data-kind]'); if (item) choose(item); });
    form.addEventListener('submit', function () { input.value = clean(input.value); remember(input.value); });
    document.addEventListener('click', function (e) { if (!form.contains(e.target)) hide(); });
    modal.addEventListener('hidden.bs.modal', function () { if (quickRequest) quickRequest.abort(); });
    document.addEventListener('error', function (event) { var img = event.target; if (img.matches && img.matches('.search-suggestions img[data-fallback]') && !img.dataset.failed) { img.dataset.failed = '1'; img.src = img.dataset.fallback; } }, true);
})(window.jQuery);
