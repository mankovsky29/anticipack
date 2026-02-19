function selectAllText(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.select();
    }
}

function scrollIntoView(element) {
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

function focusElement(element) {
    if (element) {
        element.focus();
    }
}

function scrollActiveElementIntoView(keyboardHeight) {
    const activeElement = document.activeElement;
    if (!activeElement || (activeElement.tagName !== 'INPUT' && activeElement.tagName !== 'TEXTAREA')) {
        return;
    }

    setTimeout(() => {
        const rect = activeElement.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const kbHeight = keyboardHeight || 0;
        const visibleBottom = viewportHeight - kbHeight;
        const margin = 16;

        // Check if element is inside a modal/popup
        const modalOverlay = activeElement.closest('.modal-overlay');
        const modalContent = activeElement.closest('.modal-content');

        if (modalOverlay && modalContent) {
            if (rect.bottom > visibleBottom - margin || rect.top < 0) {
                const targetTop = 100;
                const scrollAmount = rect.top - targetTop;
                modalOverlay.scrollBy({ top: scrollAmount, behavior: 'smooth' });
            }
            return;
        }

        // Only scroll if the element is outside the visible area
        if (rect.bottom > visibleBottom - margin) {
            // Element is covered by keyboard — scroll up just enough
            const scrollBy = rect.bottom - visibleBottom + margin + 40;
            window.scrollBy({ top: scrollBy, behavior: 'smooth' });
        } else if (rect.top < margin) {
            // Element is above the viewport — scroll down to reveal it
            const scrollBy = rect.top - margin - 40;
            window.scrollBy({ top: scrollBy, behavior: 'smooth' });
        }
        // Otherwise element is fully visible — do nothing
    }, 300);
}

function ensureElementVisible(element, keyboardHeight) {
    if (!element) return;
    const rect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight;
    const kbHeight = keyboardHeight || 0;
    const visibleBottom = viewportHeight - kbHeight;
    const margin = 16;

    if (rect.bottom > visibleBottom - margin) {
        const scrollBy = rect.bottom - visibleBottom + margin + 40;
        window.scrollBy({ top: scrollBy, behavior: 'smooth' });
    } else if (rect.top < margin) {
        const scrollBy = rect.top - margin - 40;
        window.scrollBy({ top: scrollBy, behavior: 'smooth' });
    }
}

// New function to adjust page padding for keyboard
function adjustPageForKeyboard(keyboardHeightDp) {
    const pageContainer = document.querySelector('.page-container');
    if (pageContainer) {
        if (keyboardHeightDp > 0) {
            // Add bottom padding equal to keyboard height
            pageContainer.style.paddingBottom = `${keyboardHeightDp}px`;
        } else {
            // Remove padding when keyboard hides
            pageContainer.style.paddingBottom = '';
        }
    }
}

// Scroll to top functionality
let dotNetHelper = null;

function initScrollToTopButton(dotNetRef) {
    dotNetHelper = dotNetRef;
    window.addEventListener('scroll', handleScroll);
    handleScroll();
}

function handleScroll() {
    if (dotNetHelper) {
        const shouldShow = window.scrollY > 200;
        dotNetHelper.invokeMethodAsync('UpdateScrollToTopVisibility', shouldShow);
    }
}

function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Theme manager functionality - Attach to window for Blazor JSInterop
window.themeManager = {
    getTheme: function() {
        try {
            const savedTheme = localStorage.getItem('theme');
            console.log('Getting theme from localStorage:', savedTheme);
            if (savedTheme) {
                return savedTheme;
            }
            
            // Check system preference
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                console.log('System prefers dark theme');
                return 'dark';
            }
            
            console.log('Defaulting to light theme');
            return 'light';
        } catch (error) {
            console.error('Error getting theme:', error);
            return 'light';
        }
    },
    
    setTheme: function(theme) {
        try {
            console.log('Setting theme to:', theme);
            localStorage.setItem('theme', theme);
            
            // Apply theme immediately
            let actualTheme = theme;
            if (theme === 'auto') {
                const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
                actualTheme = prefersDark ? 'dark' : 'light';
                console.log('Auto theme resolved to:', actualTheme);
            }
            
            // Set on both html and body to ensure it works in all contexts
            if (document.documentElement) {
                document.documentElement.setAttribute('data-theme', actualTheme);
                console.log('Set data-theme on document.documentElement');
            }
            if (document.body) {
                document.body.setAttribute('data-theme', actualTheme);
                console.log('Set data-theme on document.body');
            }
            
            // Also trigger a style recalculation
            document.documentElement.style.colorScheme = actualTheme;
            
            console.log('Theme applied successfully:', actualTheme);
            return actualTheme;
        } catch (error) {
            console.error('Error setting theme:', error);
            return theme;
        }
    }
};

// Initialize theme when DOM is ready
if (document.readyState === 'loading') {
    console.log('DOM still loading, waiting for DOMContentLoaded');
    document.addEventListener('DOMContentLoaded', function() {
        console.log('DOM loaded, initializing theme');
        const theme = window.themeManager.getTheme();
        window.themeManager.setTheme(theme);
    });
} else {
    // DOM is already ready
    console.log('DOM already ready, initializing theme immediately');
    const theme = window.themeManager.getTheme();
    window.themeManager.setTheme(theme);
}


// Auto-grow textarea functionality
function autoGrowTextarea(textarea) {
    if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
    }
}
