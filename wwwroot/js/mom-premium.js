(function () {
    const body = document.body;
    const themeKey = 'mom-theme';
    const darkClass = 'mom-theme-dark';
    const mobileSidebarClass = 'mom-sidebar-open';
    const collapsedClass = 'mom-sidebar-collapsed';

    function applyTheme(theme) {
        body.classList.toggle(darkClass, theme === 'dark');
        document.querySelectorAll('[data-mom-theme-toggle] i').forEach(function (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
    }

    function closeMobileSidebarIfNeeded() {
        if (window.innerWidth < 1200) {
            body.classList.remove(mobileSidebarClass);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        applyTheme(localStorage.getItem(themeKey) || 'light');

        document.querySelectorAll('[data-mom-theme-toggle]').forEach(function (button) {
            button.addEventListener('click', function () {
                const nextTheme = body.classList.contains(darkClass) ? 'light' : 'dark';
                localStorage.setItem(themeKey, nextTheme);
                applyTheme(nextTheme);
            });
        });

        document.querySelectorAll('[data-mom-sidebar-toggle]').forEach(function (button) {
            button.addEventListener('click', function () {
                if (window.innerWidth < 1200) {
                    body.classList.toggle(mobileSidebarClass);
                } else {
                    body.classList.toggle(collapsedClass);
                }
            });
        });

        document.querySelectorAll('.mom-alert-box').forEach(function (alertElement) {
            setTimeout(function () {
                if (window.bootstrap) {
                    bootstrap.Alert.getOrCreateInstance(alertElement).close();
                }
            }, 3500);
        });

        let deleteForm = null;
        let updateForm = null;

        const deleteModalEl = document.getElementById('deleteConfirmModal');
        const updateModalEl = document.getElementById('updateConfirmModal');
        if (!deleteModalEl || !updateModalEl || !window.bootstrap) {
            return;
        }

        const deleteModal = new bootstrap.Modal(deleteModalEl);
        const updateModal = new bootstrap.Modal(updateModalEl);
        const deleteTextEl = document.getElementById('deleteConfirmText');
        const updateTextEl = document.getElementById('updateConfirmText');
        const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
        const confirmUpdateBtn = document.getElementById('confirmUpdateBtn');

        document.addEventListener('submit', function (e) {
            const form = e.target;
            if (!(form instanceof HTMLFormElement)) return;

            if (form.dataset.confirmBypass === '1') {
                form.dataset.confirmBypass = '0';
                return;
            }

            if (form.classList.contains('js-delete-form')) {
                e.preventDefault();
                deleteForm = form;
                const msg = form.getAttribute('data-delete-message') || 'Sure to delete this record?';
                if (deleteTextEl) deleteTextEl.textContent = msg;
                deleteModal.show();
                return;
            }

            if (form.classList.contains('js-update-confirm-form')) {
                const idField = form.getAttribute('data-id-field') || '';
                if (!idField) return;

                const idInput = form.querySelector('[name="' + idField + '"]');
                if (!idInput) return;

                const idValue = (idInput.value || '').trim();
                if (idValue === '' || idValue === '0') return;

                e.preventDefault();
                updateForm = form;
                const msg = form.getAttribute('data-update-message') || 'Sure to update this record?';
                if (updateTextEl) updateTextEl.textContent = msg;
                updateModal.show();
            }
        });

        if (confirmDeleteBtn) {
            confirmDeleteBtn.addEventListener('click', function () {
                if (!deleteForm) return;
                const form = deleteForm;
                deleteForm = null;
                form.dataset.confirmBypass = '1';
                form.submit();
            });
        }

        if (confirmUpdateBtn) {
            confirmUpdateBtn.addEventListener('click', function () {
                if (!updateForm) return;
                const form = updateForm;
                updateForm = null;
                form.dataset.confirmBypass = '1';
                form.submit();
            });
        }
    });

    window.addEventListener('resize', closeMobileSidebarIfNeeded);
})();
