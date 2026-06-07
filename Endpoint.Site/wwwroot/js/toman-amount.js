(function (global) {
    'use strict';

    function digitsOnly(value) {
        return String(value || '').replace(/\D/g, '');
    }

    function formatGrouped(digits) {
        if (!digits) return '';
        return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    function syncDisplayToHidden(displayInput, hiddenInput) {
        var digits = digitsOnly(displayInput.value);
        hiddenInput.value = digits ? digits : '';
        displayInput.value = formatGrouped(digits);
    }

    function initField(displayInput) {
        var root = displayInput.closest('.toman-amount-field');
        if (!root) return;
        var hiddenInput = root.querySelector('.js-toman-amount-hidden');
        if (!hiddenInput) return;

        if (hiddenInput.value) {
            displayInput.value = formatGrouped(digitsOnly(hiddenInput.value));
        }

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
