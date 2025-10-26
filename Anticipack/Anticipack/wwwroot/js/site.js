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
 * Now: - only moves the quick-add container above the keyboard if the quick-add control (or a child) is focused
 *      - attempts to ensure the currently focused element is visible (scrolled into view) when keyboard shows
 */
window.handleKeyboardVisibility = function(isVisible, keyboardHeight) {
    const quickAddContainer = document.querySelector('.quick-add-container');
    if (!quickAddContainer) {
        // Still attempt to ensure focused element visible
        if (isVisible) {
            window.ensureElementVisible(null, keyboardHeight);
        }
        return;
    }

    // Normalize viewport and keyboard height info
    const viewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
    const kbHeightFromArg = Number(keyboardHeight) || 0;
    const inferredKb = Math.max(0, window.innerHeight - viewportHeight);
    const kbHeight = kbHeightFromArg > 0 ? kbHeightFromArg : inferredKb;

    if (isVisible) {
        // Only reposition quick-add container if it's focused (or contains the focused element)
        const isQuickAddFocused = quickAddContainer.contains(document.activeElement);

        if (isQuickAddFocused) {
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

            const containerHeight = quickAddContainer.offsetHeight || 0;
            const visibleHeight = viewportHeight;

            // Default: center within visible area
            let top = Math.floor((visibleHeight - containerHeight) * 0.5);

            // If keyboard height is known, ensure the container stays above keyboard
            if (kbHeight > 0) {
                const topOfKeyboard = visibleHeight - kbHeight;
                if ((top + containerHeight) > (topOfKeyboard - 8)) {
                    top = Math.max(8, topOfKeyboard - containerHeight - 8);
                }
            }

            top = Math.max(8, top);
            quickAddContainer.style.top = `${top}px`;
            
            // Add a small delayed re-calculation to fix initial positioning issue
            // when keyboard first appears and measurements might be unstable
            setTimeout(() => {
                const updatedViewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
                const updatedContainerHeight = quickAddContainer.offsetHeight || 0;
                let updatedTop = Math.floor((updatedViewportHeight - updatedContainerHeight) * 0.5);
                
                if (kbHeight > 0) {
                    const updatedTopOfKeyboard = updatedViewportHeight - kbHeight;
                    if ((updatedTop + updatedContainerHeight) > (updatedTopOfKeyboard - 8)) {
                        updatedTop = Math.max(8, updatedTopOfKeyboard - updatedContainerHeight - 8);
                    }
                }
                
                updatedTop = Math.max(8, updatedTop);
                quickAddContainer.style.top = `${updatedTop}px`;
            }, 100);
        } else {
            // Not focused: restore default position if we previously modified it
            if (quickAddContainer.__originalPosition) {
                quickAddContainer.style.position = quickAddContainer.__originalPosition.position;
                quickAddContainer.style.bottom = quickAddContainer.__originalPosition.bottom;
                quickAddContainer.style.top = quickAddContainer.__originalPosition.top;
                quickAddContainer.style.left = quickAddContainer.__originalPosition.left;
                quickAddContainer.style.right = quickAddContainer.__originalPosition.right;
                quickAddContainer.style.transform = quickAddContainer.__originalPosition.transform;
                quickAddContainer.__originalPosition = null;
            } else {
                // fallback clear
                quickAddContainer.style.position = '';
                quickAddContainer.style.top = '';
                quickAddContainer.style.bottom = '';
                quickAddContainer.style.left = '';
                quickAddContainer.style.right = '';
                quickAddContainer.style.transform = '';
            }
        }

        // Ensure currently focused element (if any) is visible above the keyboard
        window.ensureElementVisible(null, kbHeight);
    } else {
        // Keyboard hidden: restore quick-add container
        if (quickAddContainer.__originalPosition) {
            quickAddContainer.style.position = quickAddContainer.__originalPosition.position;
            quickAddContainer.style.bottom = quickAddContainer.__originalPosition.bottom;
            quickAddContainer.style.top = quickAddContainer.__originalPosition.top;
            quickAddContainer.style.left = quickAddContainer.__originalPosition.left;
            quickAddContainer.style.right = quickAddContainer.__originalPosition.right;
            quickAddContainer.style.transform = quickAddContainer.__originalPosition.transform;
            quickAddContainer.__originalPosition = null;
        } else {
            quickAddContainer.style.position = '';
            quickAddContainer.style.top = '';
            quickAddContainer.style.bottom = '';
            quickAddContainer.style.left = '';
            quickAddContainer.style.right = '';
            quickAddContainer.style.transform = '';
        }

        // When keyboard hides, try to restore page scroll a bit (no-op if not needed)
        window.restoreContainerPosition();
    }
};

/**
 * Ensure a given element (or the document.activeElement if null) is visible above the on-screen keyboard.
 * Accepts keyboardHeight (CSS/device pixels as passed from platform).
 */
window.ensureElementVisible = function(element, keyboardHeight) {
    try {
        const el = element || document.activeElement;
        if (!el || !el.getBoundingClientRect) return;

        const rect = el.getBoundingClientRect();
        const viewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
        const kbHeightFromArg = Number(keyboardHeight) || 0;
        const inferredKb = Math.max(0, window.innerHeight - viewportHeight);
        const kbHeight = kbHeightFromArg > 0 ? kbHeightFromArg : inferredKb;

        const padding = 12; // small gap above keyboard
        const safeBottom = viewportHeight - kbHeight - padding;

        if (rect.bottom > safeBottom) {
            const overlap = rect.bottom - safeBottom;
            // Prefer smooth scrolling; fall back to instant if not supported
            window.scrollBy({ top: overlap + padding, left: 0, behavior: 'smooth' });
        } else if (rect.top < padding) {
            // Element above the viewport top, bring it down a bit
            window.scrollBy({ top: rect.top - padding, left: 0, behavior: 'smooth' });
        } else {
            // Optionally center element if it's partially visible but covered by other overlays
            // el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    } catch (ex) {
        // silent
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