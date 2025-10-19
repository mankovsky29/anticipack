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

/**
 * Focuses on the provided element
 */
window.focusElement = function (elementRef) {
    if (elementRef && elementRef.focus) {
        elementRef.focus();
    }
};

/**
 * Sets up mobile keyboard handling for the quick add container
 */
window.setupMobileKeyboardHandling = function() {
    const quickAddContainer = document.querySelector('.quick-add-container');
    const input = document.querySelector('.quick-add-input');
    
    if (!quickAddContainer || !input) return;
    
    // Create a persistent position helper that follows the viewport
    const positionHelper = document.createElement('div');
    positionHelper.style.cssText = 'position:absolute; top:40%; left:0; width:100%; height:1px; pointer-events:none; visibility:hidden; z-index:-1;';
    document.body.appendChild(positionHelper);
    
    // Store original position directly on the element for global access
    function forceContainerToMiddle() {
        // Save original position if not already saved
        if (!quickAddContainer.__originalPosition) {
            quickAddContainer.__originalPosition = {
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
        const targetPosition = Math.floor(viewportHeight * 0.42);
        
        quickAddContainer.style.top = `${targetPosition}px`;
        quickAddContainer.style.transform = 'none'; // Clear any transforms
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
        setTimeout(window.restoreContainerPosition, 100);
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

/**
 * Restores quick-add container to its original position
 */
window.restoreContainerPosition = function() {
    const quickAddContainer = document.querySelector('.quick-add-container');
    if (!quickAddContainer) return;
    
    // Only restore if we have original position saved
    if (quickAddContainer.__originalPosition) {
        quickAddContainer.style.position = quickAddContainer.__originalPosition.position;
        quickAddContainer.style.bottom = quickAddContainer.__originalPosition.bottom;
        quickAddContainer.style.top = quickAddContainer.__originalPosition.top;
        quickAddContainer.style.left = quickAddContainer.__originalPosition.left;
        quickAddContainer.style.right = quickAddContainer.__originalPosition.right;
        quickAddContainer.style.transform = quickAddContainer.__originalPosition.transform;
        quickAddContainer.__originalPosition = null;
    }
};

/**
 * Handles keyboard visibility changes from C# code
 */
window.handleKeyboardVisibility = function(isVisible) {
    const quickAddContainer = document.querySelector('.quick-add-container');
    if (!quickAddContainer) return;
    
    if (isVisible) {
        // When keyboard is visible, make sure any dropdowns are closed
        // (Blazor component will handle this part)
    } else {
        // When keyboard hides, restore position
        window.restoreContainerPosition();
    }
};

/**
 * Determines if a dropdown should open upward based on available space
 */
window.shouldOpenDropdownUp = function(element) {
    if (!element) return false;
    
    const rect = element.getBoundingClientRect();
    const dropdownHeight = element.offsetHeight || 200; // Fallback if height not available
    const viewportHeight = window.innerHeight;
    
    // Check if there's enough space below
    const spaceBelow = viewportHeight - rect.bottom;
    
    // If we have less than dropdown height + padding, open upward
    return spaceBelow < (dropdownHeight + 20);
};