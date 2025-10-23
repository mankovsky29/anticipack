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
    
    // When input gets focus, position element in the middle of the visible area
    input.addEventListener('focus', () => {
        // Multiple positioning attempts to catch keyboard at different states
        setTimeout(window.forceContainerToMiddle, 100);
        setTimeout(window.forceContainerToMiddle, 300);
        setTimeout(window.forceContainerToMiddle, 500);
        setTimeout(window.forceContainerToMiddle, 700); // Extra delay for Samsung
    });
    
    input.addEventListener('blur', () => {
        setTimeout(window.restoreContainerPosition, 100);
    });
    
    // Also respond to resize events which happen when keyboard opens/closes
    window.addEventListener('resize', () => {
        if (document.activeElement === input) {
            setTimeout(window.forceContainerToMiddle, 50);
        }
    });
    
    // Handle orientation changes
    window.addEventListener('orientationchange', () => {
        setTimeout(() => {
            if (document.activeElement === input) {
                window.forceContainerToMiddle();
            }
        }, 300);
    });
    
    // Also handle visualViewport changes if available
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', () => {
            if (document.activeElement === input) {
                setTimeout(window.forceContainerToMiddle, 50);
            }
        });
        
        window.visualViewport.addEventListener('scroll', () => {
            if (document.activeElement === input) {
                setTimeout(window.forceContainerToMiddle, 50);
            }
        });
    }
};

/**
 * Forces the quick-add container to be positioned in the middle of the visible area
 */
window.forceContainerToMiddle = function() {
    const quickAddContainer = document.querySelector('.quick-add-container');
    if (!quickAddContainer) return;
    
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
    const targetPosition = Math.floor(viewportHeight * 0.44);
    
    quickAddContainer.style.top = `${targetPosition}px`;
    quickAddContainer.style.transform = 'none'; // Clear any transforms
}

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
 * Now accepts optional keyboardHeight (in CSS pixels or device pixels depending on platform)
 * so we can position the quick-add control above the keyboard more reliably.
 */
window.handleKeyboardVisibility = function(isVisible, keyboardHeight) {
    const quickAddContainer = document.querySelector('.quick-add-container');
    if (!quickAddContainer) return;

    // Helper: try to normalize keyboard height
    const viewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
    // If caller passed a height, use it; otherwise try to infer from window vs visualViewport
    const kbHeightFromArg = Number(keyboardHeight) || 0;
    const inferredKb = Math.max(0, window.innerHeight - viewportHeight);
    const kbHeight = kbHeightFromArg > 0 ? kbHeightFromArg : inferredKb;

    if (isVisible) {
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

        // Fix container to the viewport and position it within the visible area above the keyboard
        quickAddContainer.style.position = 'fixed';
        quickAddContainer.style.bottom = 'auto';
        quickAddContainer.style.left = '0';
        quickAddContainer.style.right = '0';
        quickAddContainer.style.transform = 'none';

        // Determine available visible height (viewport height)
        const visibleHeight = viewportHeight;
        const containerHeight = quickAddContainer.offsetHeight || 0;

        // Default: center within visible area
        let top = Math.floor((visibleHeight - containerHeight) * 0.5);

        // If keyboard height is known, ensure the container stays above keyboard
        if (kbHeight > 0) {
            const maxBottom = visibleHeight - kbHeight; // y coordinate of top of keyboard relative to viewport
            // If container would overlap keyboard, move it up so it sits just above keyboard with small padding
            if ((top + containerHeight) > (maxBottom - 8)) {
                top = Math.max(8, maxBottom - containerHeight - 8);
            }
        }

        // Clamp top to not be negative
        top = Math.max(8, top);

        quickAddContainer.style.top = `${top}px`;
    } else {
        // When keyboard hides, restore position
        if (quickAddContainer.__originalPosition) {
            quickAddContainer.style.position = quickAddContainer.__originalPosition.position;
            quickAddContainer.style.bottom = quickAddContainer.__originalPosition.bottom;
            quickAddContainer.style.top = quickAddContainer.__originalPosition.top;
            quickAddContainer.style.left = quickAddContainer.__originalPosition.left;
            quickAddContainer.style.right = quickAddContainer.__originalPosition.right;
            quickAddContainer.style.transform = quickAddContainer.__originalPosition.transform;
            quickAddContainer.__originalPosition = null;
        } else {
            // Fallback restore
            quickAddContainer.style.position = '';
            quickAddContainer.style.top = '';
            quickAddContainer.style.bottom = '';
            quickAddContainer.style.left = '';
            quickAddContainer.style.right = '';
            quickAddContainer.style.transform = '';
        }
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