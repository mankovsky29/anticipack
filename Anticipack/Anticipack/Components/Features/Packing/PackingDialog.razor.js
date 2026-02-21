export function attach(inputEl, dotNetRef, messagesEl) {
    if (!inputEl) return;
    // store the handler so detach can remove it later
    const handler = function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('SubmitFromJs');
        }
    };
    inputEl.__packingDialogHandler = handler;
    inputEl.addEventListener('keydown', handler);
    // also store messagesEl for scroll helper
    inputEl.__messagesEl = messagesEl;
}

export function detach(inputEl) {
    if (!inputEl) return;
    const handler = inputEl.__packingDialogHandler;
    if (handler) {
        inputEl.removeEventListener('keydown', handler);
        delete inputEl.__packingDialogHandler;
        delete inputEl.__messagesEl;
    }
}

export function scrollToBottom(messagesEl) {
    try {
        if (!messagesEl) return;
        messagesEl.scrollTop = messagesEl.scrollHeight;
    } catch (e) {
        // ignore
    }
}
