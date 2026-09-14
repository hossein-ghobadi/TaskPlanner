(function (global) {
    'use strict';

    var STORAGE_KEY = 'tp-display-settings-v1';

    var APP_KEYS = {
        leads: 'showLeads',
        boards: 'showBoards',
        mindmaps: 'showMindMaps',
        designs: 'showDesigns',
        adminUsers: 'showAdminUsers',
        adminLeaves: 'showAdminLeaves'
    };

    var PROJECT_KEYS = {
        'project-tasks': 'showProjectTasks',
        'project-kanban': 'showProjectKanban',
        'project-sprints': 'showProjectSprints',
        'project-features': 'showProjectFeatures',
        'project-tickets': 'showProjectTickets',
        'project-gallery': 'showProjectGallery',
        'project-categories': 'showProjectCategories'
    };

    function defaults() {
        return {
            showLeads: true,
            showBoards: true,
            showMindMaps: true,
            showDesigns: true,
            showAdminUsers: true,
            showAdminLeaves: true,
            showProjectTasks: true,
            showProjectKanban: true,
            showProjectSprints: true,
            showProjectFeatures: true,
            showProjectTickets: true,
            showProjectGallery: true,
            showProjectCategories: true
        };
    }

    function normalize(raw) {
        var base = defaults();
        if (!raw || typeof raw !== 'object') {
            return base;
        }

        Object.keys(base).forEach(function (key) {
            if (typeof raw[key] === 'boolean') {
                base[key] = raw[key];
            }
        });

        return base;
    }

    function hasStoredSettings() {
        try {
            return !!localStorage.getItem(STORAGE_KEY);
        } catch (e) {
            return false;
        }
    }

    function getSettings() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return defaults();
            }
            return normalize(JSON.parse(raw));
        } catch (e) {
            return defaults();
        }
    }

    function saveSettings(settings) {
        var normalized = normalize(settings);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
        applyNavVisibility(normalized);
        return normalized;
    }

    function syncFromServer(serverSettings) {
        return saveSettings(serverSettings);
    }

    function fetchAndStoreFromServer() {
        return fetch('/DisplaySettings/Preferences', {
            method: 'GET',
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error('failed to load display settings');
                }
                return res.json();
            })
            .then(function (data) {
                return syncFromServer(data);
            })
            .catch(function () {
                applyNavVisibility(defaults());
            });
    }

    function buildHideCss(settings) {
        var css = '';
        Object.keys(APP_KEYS).forEach(function (key) {
            if (settings[APP_KEYS[key]] === false) {
                css += '[data-display-key="' + key + '"]{display:none!important}';
            }
        });
        Object.keys(PROJECT_KEYS).forEach(function (key) {
            if (settings[PROJECT_KEYS[key]] === false) {
                css += '[data-display-key="' + key + '"]{display:none!important}';
            }
        });
        return css;
    }

    function applyInlineHideStyle(settings) {
        var css = buildHideCss(settings || getSettings());
        var style = document.getElementById('tp-display-hide');
        if (!style) {
            style = document.createElement('style');
            style.id = 'tp-display-hide';
            document.head.appendChild(style);
        }
        style.textContent = css;
    }

    function applyNavVisibility(settings) {
        settings = settings || getSettings();
        applyInlineHideStyle(settings);

        document.querySelectorAll('[data-display-key]').forEach(function (el) {
            var key = el.getAttribute('data-display-key');
            var prop = APP_KEYS[key] || PROJECT_KEYS[key];
            if (!prop) {
                return;
            }
            el.style.display = settings[prop] === false ? 'none' : '';
        });
    }

    function readFormSettings(form) {
        var settings = getSettings();
        form.querySelectorAll('input[type="checkbox"][name]').forEach(function (input) {
            var name = input.name;
            if (!name || name.indexOf('__') === 0) {
                return;
            }
            var camel = name.charAt(0).toLowerCase() + name.slice(1);
            if (Object.prototype.hasOwnProperty.call(settings, camel)) {
                settings[camel] = input.checked;
            }
        });
        return settings;
    }

    function bindDisplaySettingsForm() {
        var form = document.querySelector('form[action*="DisplaySettings"]');
        if (!form) {
            return;
        }

        form.addEventListener('submit', function () {
            saveSettings(readFormSettings(form));
        });
    }

    function bindLogoutClear() {
        var logoutForm = document.getElementById('app-sidebar-logout-form');
        if (!logoutForm) {
            return;
        }

        logoutForm.addEventListener('submit', function () {
            try {
                localStorage.removeItem(STORAGE_KEY);
            } catch (e) { /* ignore */ }
        });
    }

    function init() {
        if (global.__tpDisplaySettingsSync) {
            syncFromServer(global.__tpDisplaySettingsSync);
            delete global.__tpDisplaySettingsSync;
        } else if (hasStoredSettings()) {
            applyNavVisibility();
        } else {
            fetchAndStoreFromServer();
        }

        bindDisplaySettingsForm();
        bindLogoutClear();
    }

    global.tpDisplaySettings = {
        STORAGE_KEY: STORAGE_KEY,
        defaults: defaults,
        hasStoredSettings: hasStoredSettings,
        getSettings: getSettings,
        saveSettings: saveSettings,
        syncFromServer: syncFromServer,
        fetchAndStoreFromServer: fetchAndStoreFromServer,
        applyInlineHideStyle: applyInlineHideStyle,
        applyNavVisibility: applyNavVisibility,
        init: init
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})(window);
