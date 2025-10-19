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

window.setupMobileKeyboardHandling = function() {
    const quickAddContainer = document.querySelector('.quick-add-container');
    const input = document.querySelector('.quick-add-input');
    
    if (!quickAddContainer || !input) return;
    
    // Create a persistent position helper that follows the viewport
    const positionHelper = document.createElement('div');
    positionHelper.style.cssText = 'position:absolute; top:40%; left:0; width:100%; height:1px; pointer-events:none; visibility:hidden; z-index:-1;';
    document.body.appendChild(positionHelper);
    
    // Position tracking
    let originalPosition = null;
    let originalContainerStyles = {
        position: quickAddContainer.style.position,
        bottom: quickAddContainer.style.bottom,
        left: quickAddContainer.style.left,
        right: quickAddContainer.style.right
    };
    
    function forceContainerToMiddle() {
        // Save original position if not already saved
        if (!originalPosition) {
            originalPosition = {
                position: quickAddContainer.style.position,
                bottom: quickAddContainer.style.bottom,
                top: quickAddContainer.style.top,
                left: quickAddContainer.style.left,
                right: quickAddContainer.style.right,
                transform: quickAddContainer.style.transform
            };
        }
        
        // Force the container to be positioned in the middle of the visible area
        quickAddContainer.style.position = 'fixed';
        quickAddContainer.style.bottom = 'auto';
        quickAddContainer.style.left = '0';
        quickAddContainer.style.right = '0';
        
        // Calculate position to be at ~40% from top of current viewport
        // This places it in the middle of the visible area when keyboard is open
        const viewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
        const targetPosition = Math.floor(viewportHeight * 0.4);
        
        quickAddContainer.style.top = `${targetPosition}px`;
        quickAddContainer.style.transform = 'none'; // Clear any transforms
    }
    
    function restoreContainerPosition() {
        // Only restore if we have original position saved
        if (originalPosition) {
            quickAddContainer.style.position = originalPosition.position;
            quickAddContainer.style.bottom = originalPosition.bottom;
            quickAddContainer.style.top = originalPosition.top;
            quickAddContainer.style.left = originalPosition.left;
            quickAddContainer.style.right = originalPosition.right;
            quickAddContainer.style.transform = originalPosition.transform;
            originalPosition = null;
        }
    }
    
    // When input gets focus, position element in the middle of the visible area
    input.addEventListener('focus', () => {
        // Multiple positioning attempts to catch keyboard at different states
        setTimeout(forceContainerToMiddle, 100);
        setTimeout(forceContainerToMiddle, 300);
        setTimeout(forceContainerToMiddle, 500);
        setTimeout(forceContainerToMiddle, 700); // Extra delay for Samsung
    });
    
    input.addEventListener('blur', () => {
        setTimeout(restoreContainerPosition, 100);
    });
    
    // Also respond to resize events which happen when keyboard opens/closes
    window.addEventListener('resize', () => {
        if (document.activeElement === input) {
            setTimeout(forceContainerToMiddle, 50);
        }
    });
    
    // Handle orientation changes
    window.addEventListener('orientationchange', () => {
        setTimeout(() => {
            if (document.activeElement === input) {
                forceContainerToMiddle();
            }
        }, 300);
    });
    
    // Also handle visualViewport changes if available
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', () => {
            if (document.activeElement === input) {
                setTimeout(forceContainerToMiddle, 50);
            }
        });
        
        window.visualViewport.addEventListener('scroll', () => {
            if (document.activeElement === input) {
                setTimeout(forceContainerToMiddle, 50);
            }
        });
    }
};