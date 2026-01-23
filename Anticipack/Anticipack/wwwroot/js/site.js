function selectAllText(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.select();
    }
}

function focusElement(element) {
    if (element) {
        setTimeout(() => {
            element.focus();
        }, 100);
    }
}


function focusElement(element) {
    if (element) {
        element.focus();
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
