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

function blurQuickAddInput() {
    const activeElement = document.activeElement;
    if (activeElement && activeElement.tagName === 'INPUT') {
        activeElement.blur();
    }
}

function scrollActiveElementIntoView() {
    const activeElement = document.activeElement;
    if (activeElement && (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA')) {
        setTimeout(() => {
            // Check if element is inside a modal/popup - don't scroll the background page
            const isInModal = activeElement.closest('.modal-overlay, .modal-content, .add-item-form');
            if (isInModal) {
                // For modals, just ensure element is visible within the modal
                activeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
                return;
            }
            
            // If element is in a fixed positioned container (like quick-add), don't scroll
            const isInFixedContainer = activeElement.closest('.quick-add-container, .quick-add-form');
            if (isInFixedContainer) {
                // Don't scroll - fixed elements manage their own position
                return;
            }
            
            // Get element position
            const rect = activeElement.getBoundingClientRect();
            const absoluteTop = rect.top + window.pageYOffset;
            
            // Calculate scroll position to place input at 1/3 from top of viewport
            // This gives enough space above for context and keeps it well above keyboard
            const viewportHeight = window.innerHeight;
            const targetPosition = absoluteTop - (viewportHeight / 3);
            
            // Scroll to calculated position
            window.scrollTo({
                top: Math.max(0, targetPosition),
                behavior: 'smooth'
            });
        }, 350); // Delay to allow keyboard animation to complete
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
