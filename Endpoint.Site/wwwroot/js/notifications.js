// =====================================================================
// مدیریت اعلان‌ها و سایدبار موبایل
// قبلاً به صورت inline داخل _Layout.cshtml بود — حالا جدا شده
// تا مرورگر بتواند آن را cache کند (long-lived cache + asp-append-version).
// =====================================================================

document.addEventListener('DOMContentLoaded', function () {
    const notificationBell = document.getElementById('notificationBell');
    if (!notificationBell) {
        return;
    }

    const badge = document.getElementById('notificationBadge');
    const listContainer = document.getElementById('notificationList');
    const emptyState = document.getElementById('notificationEmpty');
    const markAllForm = document.getElementById('markAllNotificationsForm');
    const refreshBtn = document.getElementById('refreshNotificationsBtn');
    if (!listContainer || !emptyState) {
        return;
    }
    const requestToken = document.body.dataset.antiforgeryToken;
    let isLoading = false;

    async function loadCount() {
        try {
            const response = await fetch('/Notifications/UnreadCount');
            if (!response.ok) {
                throw new Error('response failed');
            }
            const data = await response.json();
            updateBadge(data.count ?? 0);
        } catch (error) {
            console.warn('خطا در دریافت تعداد نوتیفیکیشن‌ها', error);
        }
    }

    async function loadLatest(force = false) {
        if (isLoading && !force) {
            return;
        }
        isLoading = true;
        showLoadingState();
        try {
            const response = await fetch('/Notifications/Latest?take=10');
            if (!response.ok) {
                throw new Error('response failed');
            }
            const data = await response.json();
            renderNotifications(Array.isArray(data) ? data : []);
        } catch (error) {
            console.warn('خطا در دریافت لیست نوتیفیکیشن‌ها', error);
            showErrorState();
        } finally {
            isLoading = false;
        }
    }

    async function markAsRead(id, element) {
        try {
            const response = await fetch('/Notifications/MarkAsRead', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': requestToken,
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8'
                },
                body: `id=${encodeURIComponent(id)}`
            });
            if (!response.ok) {
                throw new Error('response failed');
            }
            element?.classList.remove('notification-item-unread');
            await loadCount();
            await loadLatest(true);
        } catch (error) {
            console.warn('خطا در علامت‌گذاری نوتیفیکیشن', error);
        }
    }

    function renderNotifications(notifications) {
        listContainer.innerHTML = '';
        if (!notifications.length) {
            const emptyBlock = document.createElement('div');
            emptyBlock.className = 'notification-empty';
            emptyBlock.innerHTML = '<i class="bi bi-bell-slash"></i>نوتیفیکیشن جدیدی وجود ندارد.';
            listContainer.appendChild(emptyBlock);
            return;
        }

        notifications.forEach(notification => {
            const item = document.createElement('button');
            item.type = 'button';
            item.className = `notification-item w-100 text-start ${notification.isRead ? '' : 'notification-item-unread'}`;
            item.dataset.id = notification.id;
            item.innerHTML = `
                <div class="notification-item-title">${notification.title}</div>
                <div class="notification-item-message">${notification.message}</div>
                <div class="notification-item-time">
                    <i class="bi bi-clock"></i>
                    <span>${formatTehranPersianDateTime(notification.createdAt)}</span>
                </div>
            `;
            item.addEventListener('click', async () => {
                await markAsRead(notification.id, item);
                navigateByPayload(notification);
            });
            listContainer.appendChild(item);
        });
    }

    function navigateByPayload(notification) {
        if (!notification.payloadJson) {
            return;
        }
        try {
            const payload = JSON.parse(notification.payloadJson);
            if (payload?.taskId) {
                window.location.href = `/Tasks/Details/${payload.taskId}`;
                return;
            }
            if (payload?.projectId && notification.type === 'ProjectInvitation') {
                window.location.href = `/Projects/Details/${payload.projectId}`;
            }
        } catch (error) {
            console.warn('payload parse failed', error);
        }
    }

    function showLoadingState() {
        listContainer.innerHTML = '';
        const spinner = document.createElement('div');
        spinner.className = 'notification-empty';
        spinner.innerHTML = '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div><div class="mt-2">در حال بارگذاری...</div>';
        listContainer.appendChild(spinner);
    }

    function showErrorState() {
        listContainer.innerHTML = '';
        const errorBlock = document.createElement('div');
        errorBlock.className = 'notification-empty';
        errorBlock.innerHTML = '<i class="bi bi-exclamation-triangle"></i>خطا در دریافت اطلاعات.';
        listContainer.appendChild(errorBlock);
    }

    function updateBadge(count) {
        if (!badge) {
            return;
        }
        const numericCount = Number(count) || 0;
        if (numericCount > 0) {
            badge.textContent = numericCount > 9 ? '9+' : numericCount;
            badge.classList.remove('d-none');
        } else {
            badge.textContent = '0';
            badge.classList.add('d-none');
        }
    }

    function formatTehranPersianDateTime(dateString) {
        try {
            const date = new Date(dateString);
            if (Number.isNaN(date.getTime())) {
                return '';
            }
            return new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
                timeZone: 'Asia/Tehran',
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false
            }).format(date);
        } catch {
            return '';
        }
    }

    const notificationPanel = document.getElementById('notificationPanel');
    const notificationBackdrop = document.getElementById('notificationBackdrop');

    function openNotifications() {
        const isMobile = window.innerWidth < 992;
        notificationPanel?.classList.add('is-open');
        notificationBell?.setAttribute('aria-expanded', 'true');
        if (isMobile) {
            // در موبایل بک‌دراپ و قفل اسکرول غیرفعال است تا تداخل کلیک رخ ندهد.
            notificationBackdrop?.classList.remove('is-open');
        } else {
            notificationBackdrop?.classList.add('is-open');
            document.body.classList.add('notifications-open');
        }
        loadLatest(true);
    }

    function closeNotifications() {
        notificationPanel?.classList.remove('is-open');
        notificationBackdrop?.classList.remove('is-open');
        notificationBell?.setAttribute('aria-expanded', 'false');
        document.body.classList.remove('notifications-open');
    }

    notificationBell?.addEventListener('click', () => {
        const isOpen = notificationPanel?.classList.contains('is-open');
        if (isOpen) {
            closeNotifications();
        } else {
            openNotifications();
        }
    });

    notificationBackdrop?.addEventListener('click', closeNotifications);
    refreshBtn?.addEventListener('click', () => loadLatest(true));

    markAllForm?.addEventListener('submit', async (event) => {
        event.preventDefault();
        try {
            const response = await fetch('/Notifications/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': requestToken
                }
            });
            if (!response.ok) {
                throw new Error('response failed');
            }
            await loadCount();
            await loadLatest(true);
        } catch (error) {
            console.warn('خطا در علامت‌گذاری همه نوتیفیکیشن‌ها', error);
        }
    });

    loadCount();
    setInterval(loadCount, 60000);

    // Mobile sidebar (hamburger)
    const mobileToggle = document.getElementById('mobileSidebarToggle');
    const mobileBackdrop = document.getElementById('mobileSidebarBackdrop');
    const mobileClose = document.getElementById('mobileSidebarClose');
    const appShell = document.querySelector('.app-shell');
    const mobileNavLinks = document.querySelectorAll('.app-sidebar-nav a');

    function closeMobileSidebar() {
        appShell?.classList.remove('sidebar-open');
        document.body.classList.remove('mobile-menu-open');
    }

    function resetInteractiveOverlays() {
        closeMobileSidebar();
        closeNotifications();
    }

    // وقتی صفحه با back/forward cache برمی‌گردد، گاهی overlayها باز می‌مانند.
    // این ریست از گیر افتادن کلیک‌ها جلوگیری می‌کند.
    resetInteractiveOverlays();
    window.addEventListener('pageshow', resetInteractiveOverlays);

    mobileToggle?.addEventListener('click', function () {
        appShell?.classList.add('sidebar-open');
        document.body.classList.add('mobile-menu-open');
    });

    mobileClose?.addEventListener('click', closeMobileSidebar);
    mobileBackdrop?.addEventListener('click', closeMobileSidebar);
    mobileNavLinks.forEach(link => link.addEventListener('click', closeMobileSidebar));

    window.addEventListener('resize', () => {
        if (window.innerWidth >= 992) {
            closeMobileSidebar();
        }
    });
});
