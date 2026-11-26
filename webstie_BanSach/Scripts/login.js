(function () {
    'use strict';
    var form = document.getElementById('login-form'), email = document.getElementById('login-email'), password = document.getElementById('login-password');
    var toggle = document.getElementById('password-toggle'), caps = document.getElementById('caps-warning'), button = document.getElementById('login-submit');
    toggle.addEventListener('click', function () { var show = password.type === 'password'; password.type = show ? 'text' : 'password'; toggle.textContent = show ? 'Ẩn' : 'Hiện'; toggle.setAttribute('aria-pressed', show ? 'true' : 'false'); toggle.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'); });
    function checkCaps(event) { if (event.getModifierState) caps.hidden = !event.getModifierState('CapsLock'); }
    password.addEventListener('keyup', checkCaps); password.addEventListener('keydown', checkCaps); password.addEventListener('blur', function () { caps.hidden = true; });
    function error(id, message) { document.getElementById(id).textContent = message; }
    email.addEventListener('input', function () { error('email-error', ''); }); password.addEventListener('input', function () { error('password-error', ''); });
    form.addEventListener('submit', function (event) {
        email.value = email.value.trim(); var valid = true;
        if (!email.value || !email.validity.valid) { error('email-error', email.value ? 'Vui lòng nhập email hợp lệ.' : 'Vui lòng nhập email.'); valid = false; }
        if (!password.value) { error('password-error', 'Vui lòng nhập mật khẩu.'); valid = false; }
        if (!valid) { event.preventDefault(); (!email.value || !email.validity.valid ? email : password).focus(); return; }
        button.disabled = true; button.querySelector('.login-spinner').hidden = false;
    });
    window.addEventListener('pageshow', function () { button.disabled = false; button.querySelector('.login-spinner').hidden = true; });
})();
