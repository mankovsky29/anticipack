let _clickOutsideHandler = null;
let _overflowClickOutsideHandler = null;

export function scrollToAddForm() {
    requestAnimationFrame(function () {
        const form = document.querySelector('.add-item-form');
        if (!form) return;

        const rect = form.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const margin = 32;

        // Only scroll if the form is not fully visible in the viewport
        if (rect.top < margin || rect.bottom > viewportHeight - margin) {
            form.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });
}

export function registerClickOutsideHandler(dotNetRef) {
    unregisterClickOutsideHandler();
    _clickOutsideHandler = function (event) {
        const editContainer = document.querySelector('.item-edit-container');
        const addForm = document.querySelector('.add-item-form');
        const insideEdit = editContainer != null && editContainer.contains(event.target);
        const insideAdd = addForm != null && addForm.contains(event.target);
        if (!insideEdit && !insideAdd) {
            dotNetRef.invokeMethodAsync('CloseFlyoutsByClickOffJsAsync');
        }
    };
    // Defer by one frame so the click that opened the form is not intercepted.
    requestAnimationFrame(function () {
        document.addEventListener('click', _clickOutsideHandler, true);
    });
}

export function unregisterClickOutsideHandler() {
    if (_clickOutsideHandler != null) {
        document.removeEventListener('click', _clickOutsideHandler, true);
        _clickOutsideHandler = null;
    }
}

export function positionOverflowMenu() {
    const menu = document.querySelector('.action-dropdown');
    if (!menu) return;

    menu.classList.remove('open-up');

    const rect = menu.getBoundingClientRect();
    const viewportHeight = window.innerHeight;
    const margin = 8;

    if (rect.bottom > viewportHeight - margin) {
        menu.classList.add('open-up');
    }
}

export function registerOverflowClickOutside(dotNetRef) {
    unregisterOverflowClickOutside();
    _overflowClickOutsideHandler = function (event) {
        const dropdown = document.querySelector('.action-dropdown');
        const overflowBtn = event.target.closest('.overflow-btn');
        if (!dropdown || (!dropdown.contains(event.target) && !overflowBtn)) {
            dotNetRef.invokeMethodAsync('CloseOverflowMenuFromJsAsync');
        }
    };
    // Defer by one frame so the click that opened the menu is not intercepted.
    requestAnimationFrame(function () {
        document.addEventListener('click', _overflowClickOutsideHandler, true);
    });
}

export function unregisterOverflowClickOutside() {
    if (_overflowClickOutsideHandler) {
        document.removeEventListener('click', _overflowClickOutsideHandler, true);
        _overflowClickOutsideHandler = null;
    }
}
