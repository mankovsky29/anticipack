let _menuClickOutsideHandler = null;

export function positionMenu() {
    const menu = document.querySelector('.menu-dropdown');
    if (!menu) return;

    menu.classList.remove('open-up');

    const rect = menu.getBoundingClientRect();
    const viewportHeight = window.innerHeight;
    const margin = 8;

    if (rect.bottom > viewportHeight - margin) {
        menu.classList.add('open-up');
    }
}

export function registerMenuClickOutside(dotNetRef) {
    unregisterMenuClickOutside();
    _menuClickOutsideHandler = function (event) {
        const dropdown = document.querySelector('.menu-dropdown');
        const menuBtn = event.target.closest('.btn-menu');
        if (!dropdown || (!dropdown.contains(event.target) && !menuBtn)) {
            dotNetRef.invokeMethodAsync('CloseMenuFromJsAsync');
        }
    };
    // Defer by one frame so the click that opened the menu is not intercepted.
    requestAnimationFrame(function () {
        document.addEventListener('click', _menuClickOutsideHandler, true);
    });
}

export function unregisterMenuClickOutside() {
    if (_menuClickOutsideHandler) {
        document.removeEventListener('click', _menuClickOutsideHandler, true);
        _menuClickOutsideHandler = null;
    }
}
