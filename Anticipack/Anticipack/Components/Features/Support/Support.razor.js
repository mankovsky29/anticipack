export function attach(inputEl, dotNetRef, messagesEl) {
    if (!inputEl) return;

    // Fit page container to available viewport height so input stays visible
    const container = inputEl.closest('.page-container');
    if (container) {
        const fitToViewport = () => {
            container.style.height = '';
            const top = container.getBoundingClientRect().top;
            container.style.height = (window.innerHeight - top) + 'px';
        };
        fitToViewport();
        inputEl.__fitToViewport = fitToViewport;
        window.addEventListener('resize', fitToViewport);
    }

    // Auto-resize textarea
    const autoResize = function () {
        inputEl.style.height = 'auto';
        inputEl.style.height = Math.min(inputEl.scrollHeight, 120) + 'px';
    };

    const keydownHandler = function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('SubmitFromJs');
        }
    };

    inputEl.__supportAutoResize = autoResize;
    inputEl.__supportKeydownHandler = keydownHandler;
    inputEl.addEventListener('input', autoResize);
    inputEl.addEventListener('keydown', keydownHandler);
    inputEl.__messagesEl = messagesEl;
}

export function detach(inputEl) {
    if (!inputEl) return;
    if (inputEl.__fitToViewport) {
        window.removeEventListener('resize', inputEl.__fitToViewport);
        delete inputEl.__fitToViewport;
    }
    if (inputEl.__supportAutoResize) {
        inputEl.removeEventListener('input', inputEl.__supportAutoResize);
        delete inputEl.__supportAutoResize;
    }
    if (inputEl.__supportKeydownHandler) {
        inputEl.removeEventListener('keydown', inputEl.__supportKeydownHandler);
        delete inputEl.__supportKeydownHandler;
    }
    delete inputEl.__messagesEl;
}

export function scrollToBottom(messagesEl) {
    try {
        if (!messagesEl) return;
        messagesEl.scrollTop = messagesEl.scrollHeight;
    } catch (e) {
        // ignore
    }
}
