(function (global) {
    'use strict';

    function digitsOnly(value) {
        return String(value || '').replace(/\D/g, '');
    }

    function parseAmountDigits(value) {
        var s = String(value || '').trim();
        if (!s) return '';
        if (/^\d+$/.test(s)) return s;
        var normalized = s.replace(/,/g, '');
        var num = parseFloat(normalized);
        if (Number.isFinite(num) && num >= 0) {
            return String(Math.round(num));
        }
        return digitsOnly(s);
    }

    function formatGrouped(digits) {
        if (!digits) return '';
        return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    function syncDisplayToHidden(displayInput, hiddenInput) {
        var digits = parseAmountDigits(displayInput.value);
        hiddenInput.value = digits ? digits : '';
        displayInput.value = formatGrouped(digits);
    }

    function initField(displayInput) {
        var root = displayInput.closest('.toman-amount-field');
        if (!root) return;
        var hiddenInput = root.querySelector('.js-toman-amount-hidden');
        if (!hiddenInput) return;

        var digits = parseAmountDigits(hiddenInput.value) || parseAmountDigits(displayInput.value);
        hiddenInput.value = digits;
        displayInput.value = formatGrouped(digits);

        displayInput.addEventListener('input', function () {
            syncDisplayToHidden(displayInput, hiddenInput);
        });

        displayInput.addEventListener('blur', function () {
            syncDisplayToHidden(displayInput, hiddenInput);
        });
    }

    function initRoot(root) {
        if (!root) return;
        root.querySelectorAll('.js-toman-amount-display').forEach(initField);
    }

    function prepareForm(form) {
        if (!form) return;
        form.querySelectorAll('.toman-amount-field').forEach(function (field) {
            var displayInput = field.querySelector('.js-toman-amount-display');
            var hiddenInput = field.querySelector('.js-toman-amount-hidden');
            if (displayInput && hiddenInput) {
                syncDisplayToHidden(displayInput, hiddenInput);
            }
        });
    }

    global.TomanAmount = {
        init: initRoot,
        prepareForm: prepareForm
    };

    document.addEventListener('DOMContentLoaded', function () {
        initRoot(document);
        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function () {
                prepareForm(form);
            });
        });
    });
})(window);
