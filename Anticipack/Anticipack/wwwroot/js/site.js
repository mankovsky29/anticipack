window.selectAllText = function (el) {
    if (!el) return;
    // Works for input/textarea
    if (typeof el.select === "function") {
        el.focus();
        el.select();
        try {
            el.setSelectionRange(0, (el.value ?? "").length);
        } catch { /* ignored */ }
        return;
    }
    // Fallback for other elements
    const range = document.createRange();
    range.selectNodeContents(el);
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(range);
};

window.focusElement = function (elementRef) {
    if (elementRef && elementRef.focus) {
        elementRef.focus();
    }
};