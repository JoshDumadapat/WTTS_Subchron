// PH mobile: format as 0985 458 5456 (max 11 digits: 0 + 10). Use with input inside a wrapper that shows +63 prefix.
(function () {
    function formatPhMobile(input) {
        var v = input.value.replace(/\D/g, '');
        if (v.indexOf('63') === 0) v = v.substring(2);
        v = v.substring(0, 11);
        if (v.length === 0) { input.value = ''; return; }
        if (v.charAt(0) !== '0') v = '0' + v.substring(0, 10);
        var out = v.substring(0, 4);
        if (v.length > 4) out += ' ' + v.substring(4, 7);
        if (v.length > 7) out += ' ' + v.substring(7, 11);
        input.value = out;
    }
    function init() {
        document.querySelectorAll('.ph-phone-input').forEach(function (el) {
            if (el.dataset.phPhoneInit) return;
            el.dataset.phPhoneInit = '1';
            el.setAttribute('maxlength', '14');
            el.addEventListener('input', function () { formatPhMobile(this); });
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
