(function (window, $) {
  'use strict';

  if (!$) return;

  function getStep(list) {
    if (!list) return 0;
    var first = list.querySelector('.best-sellers-scroll__item');
    if (!first) return 0;
    var styles = window.getComputedStyle(list);
    var gap = parseFloat(styles.gap || styles.columnGap || '0');
    var width = first.getBoundingClientRect().width;
    return Math.max(width + (Number.isFinite(gap) ? gap : 0), 220);
  }

  function wireSlider(list, $prevBtn, $nextBtn) {
    if (!list || !$prevBtn.length || !$nextBtn.length) return;

    var move = function (direction) {
      var step = getStep(list);
      if (!step) return;
      var target = list.scrollLeft + direction * step;
      if (typeof list.scrollTo === 'function') {
        try {
          list.scrollTo({ left: target, behavior: 'smooth' });
          return;
        } catch (e) {  }
      }
      list.scrollLeft = target;
    };

    $prevBtn.off('click').on('click', function () { move(-1); });
    $nextBtn.off('click').on('click', function () { move(1); });
  }

  function getCategoryStep(list) {
    if (!list) return 0;
    var first = list.querySelector('.categories-scroll__item');
    if (!first) return 0;
    var styles = window.getComputedStyle(list);
    var gap = parseFloat(styles.gap || styles.columnGap || '0');
    var width = first.getBoundingClientRect().width;
    return Math.max(width + (Number.isFinite(gap) ? gap : 0), 220);
  }

  function wireCategoriesSlider(list, $prevBtn, $nextBtn) {
    if (!list || !$prevBtn.length || !$nextBtn.length) return;

    var move = function (direction) {
      var step = getCategoryStep(list);
      if (!step) return;
      var target = list.scrollLeft + direction * step;
      if (typeof list.scrollTo === 'function') {
        try {
          list.scrollTo({ left: target, behavior: 'smooth' });
          return;
        } catch (e) { /* ignore */ }
      }
      list.scrollLeft = target;
    };

    $prevBtn.off('click').on('click', function () { move(-1); });
    $nextBtn.off('click').on('click', function () { move(1); });
  }

  function initBestSellers() {
    var $shell = $('.best-sellers-shell').first();
    if (!$shell.length) return;

    var endpoint = $shell.data('endpoint');
    if (!endpoint) return;

    var $slider = $shell.find('[data-best-sellers-shell]');
    var $listHost = $shell.find('[data-best-sellers-container]');
    var $loading = $shell.find('#best-sellers-loading');
    var $error = $shell.find('#best-sellers-error');
    var $errorMsg = $shell.find('[data-best-sellers-error]');
    var $retryBtn = $shell.find('[data-best-sellers-retry]');
    var $empty = $shell.find('#best-sellers-empty');
    var $prevBtn = $shell.find('[data-best-sellers-prev]');
    var $nextBtn = $shell.find('[data-best-sellers-next]');

    function showState(state) {
      $loading.toggleClass('d-none', state !== 'loading');
      $error.toggleClass('d-none', state !== 'error');
      $empty.toggleClass('d-none', state !== 'empty');
      $slider.toggleClass('d-none', state !== 'ready');
    }

    function load() {
      showState('loading');
      $listHost.empty();

      $.ajax({ url: endpoint, method: 'GET', cache: false })
        .done(function (html) {
          $listHost.html(html || '');

          var list = $listHost.find('.best-sellers-scroll').get(0);
          var hasItems = !!(list && list.querySelector('.best-sellers-scroll__item'));
          if (!hasItems) {
            showState('empty');
            return;
          }

          showState('ready');
          wireSlider(list, $prevBtn, $nextBtn);
          if (window.preloadWishlist) window.preloadWishlist();
        })
        .fail(function () {
          $errorMsg.text('Khong the tai sach ban chay.');
          showState('error');
        });
    }

    $retryBtn.off('click').on('click', load);
    load();
  }

  function initCategories() {
    var $shell = $('.categories-shell').first();
    if (!$shell.length) return;

    var endpoint = $shell.data('endpoint');
    if (!endpoint) return;

    var $slider = $shell.find('[data-categories-shell]');
    var $scroll = $shell.find('[data-categories-container]');
    var $loading = $shell.find('#categories-loading');
    var $error = $shell.find('#categories-error');
    var $errorMsg = $shell.find('[data-categories-error]');
    var $retryBtn = $shell.find('[data-categories-retry]');
    var $empty = $shell.find('#categories-empty');
    var $prevBtn = $shell.find('[data-categories-prev]');
    var $nextBtn = $shell.find('[data-categories-next]');

    function showState(state) {
      $loading.toggleClass('d-none', state !== 'loading');
      $error.toggleClass('d-none', state !== 'error');
      $empty.toggleClass('d-none', state !== 'empty');
      $slider.toggleClass('d-none', state !== 'ready');
    }

    function load() {
      showState('loading');
      $scroll.empty();

      $.ajax({ url: endpoint, method: 'GET', cache: false })
        .done(function (html) {
          $scroll.html(html || '');
          var hasItems = $scroll.find('.categories-scroll__item').length > 0;

          if (!hasItems) {
            showState('empty');
            return;
          }

          showState('ready');
          wireCategoriesSlider($scroll.get(0), $prevBtn, $nextBtn);
        })
        .fail(function () {
          $errorMsg.text('Khong the tai danh muc.');
          showState('error');
        });
    }

    $retryBtn.off('click').on('click', load);
    load();
  }

  function initNewest() {
    var $shell = $('.newest-shell').first();
    if (!$shell.length) return;

    var endpoint = $shell.data('endpoint');
    if (!endpoint) return;

    var $listHost = $shell.find('[data-newest-container]');
    var $loading = $shell.find('#newest-loading');
    var $error = $shell.find('#newest-error');
    var $errorMsg = $shell.find('[data-newest-error]');
    var $retryBtn = $shell.find('[data-newest-retry]');
    var $empty = $shell.find('#newest-empty');

    function showState(state) {
      $loading.toggleClass('d-none', state !== 'loading');
      $error.toggleClass('d-none', state !== 'error');
      $empty.toggleClass('d-none', state !== 'empty');
      $listHost.toggleClass('d-none', state !== 'ready');
    }

    function load() {
      showState('loading');
      $listHost.empty();

      $.ajax({ url: endpoint, method: 'GET', cache: false })
        .done(function (html) {
          $listHost.html(html || '');
          var hasItems = $listHost.find('.col').length > 0;

          if (!hasItems) {
            showState('empty');
            return;
          }

          showState('ready');
          if (window.preloadWishlist) window.preloadWishlist();
        })
        .fail(function () {
          $errorMsg.text('Khong the tai sach moi nhat.');
          showState('error');
        });
    }

    $retryBtn.off('click').on('click', load);
    load();
  }

  $(function () {
    initBestSellers();
    initCategories();
    initNewest();
  });
})(window, window.jQuery || window.$);

document.addEventListener('error', function (event) {
  var image = event.target;
  if (image.matches && image.matches('.book-card__img[data-fallback]') && !image.dataset.fallbackUsed) {
    image.dataset.fallbackUsed = 'true';
    image.src = image.dataset.fallback;
  }
}, true);