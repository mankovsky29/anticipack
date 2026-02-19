let _clickOutsideHandler = null;

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
