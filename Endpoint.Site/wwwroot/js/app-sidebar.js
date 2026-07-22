document.addEventListener('DOMContentLoaded', function () {
    const appShell = document.querySelector('.app-shell');
    const sidebar = document.querySelector('[data-app-sidebar]');
    const toggleBtn = document.querySelector('[data-app-sidebar-toggle]');
    const icon = toggleBtn?.querySelector('i');
    const desktopMedia = window.matchMedia('(min-width: 992px)');

    if (!appShell || !sidebar || !toggleBtn) {
        return;
    }

    const isDesktop = () => desktopMedia.matches;

    const updateIcon = (collapsed) => {
        if (!icon) {
            return;
        }

        icon.className = collapsed
            ? 'bi bi-layout-sidebar-inset'
            : 'bi bi-layout-sidebar-inset-reverse';
    };

    const setCollapsed = (collapsed) => {
        if (!isDesktop()) {
            appShell.classList.remove('app-sidebar-collapsed');
            sidebar.classList.remove('is-collapsed');
            toggleBtn.setAttribute('aria-expanded', 'true');
            toggleBtn.setAttribute('aria-label', 'جمع کردن سایدبار');
            updateIcon(false);
            return;
        }

        appShell.classList.toggle('app-sidebar-collapsed', collapsed);
        sidebar.classList.toggle('is-collapsed', collapsed);
        toggleBtn.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
        toggleBtn.setAttribute('aria-label', collapsed ? 'باز کردن سایدبار' : 'جمع کردن سایدبار');
        updateIcon(collapsed);
    };

    toggleBtn.addEventListener('click', function () {
        setCollapsed(!appShell.classList.contains('app-sidebar-collapsed'));
    });

    desktopMedia.addEventListener('change', function () {
        setCollapsed(appShell.classList.contains('app-sidebar-collapsed'));
    });
});
