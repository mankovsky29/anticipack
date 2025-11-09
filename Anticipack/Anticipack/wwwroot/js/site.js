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
 * Blurs the quick-add input to dismiss the keyboard
 */
window.blurQuickAddInput = function() {
    const quickAddInput = document.querySelector('.quick-add-input');
    if (quickAddInput && document.activeElement === quickAddInput) {
        quickAddInput.blur();
    }
};

/**
 * Initialize swipe-to-reveal functionality for packing items
 */
window.initializeSwipeHandlers = function() {
    let touchStartX = 0;
    let touchStartY = 0;
    let currentSwipedItem = null;
    let isSwiping = false;
    const SWIPE_REVEAL_WIDTH = 112; // Width of both buttons (56px each)

    document.addEventListener('touchstart', function(e) {
        const packingRow = e.target.closest('.packing-row');
        if (!packingRow || packingRow.classList.contains('editing')) return;

        touchStartX = e.touches[0].clientX;
        touchStartY = e.touches[0].clientY;
        isSwiping = false;
    }, { passive: true });

    document.addEventListener('touchmove', function(e) {
        const packingRow = e.target.closest('.packing-row');
        if (!packingRow || packingRow.classList.contains('editing')) return;

        const rowContent = packingRow.querySelector('.packing-row-content');
        if (!rowContent) return;

        const touchX = e.touches[0].clientX;
        const touchY = e.touches[0].clientY;
        const deltaX = touchX - touchStartX;
        const deltaY = touchY - touchStartY;

        // Determine if this is a horizontal swipe
        if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > 10) {
            isSwiping = true;
            
            // Prevent scrolling while swiping
            e.preventDefault();

            // Close other swiped items
            if (currentSwipedItem && currentSwipedItem !== packingRow) {
                const otherContent = currentSwipedItem.querySelector('.packing-row-content');
                if (otherContent) {
                    otherContent.style.transform = '';
                }
                currentSwipedItem.classList.remove('swiped-left', 'swiped-right');
            }

            // Apply transform to the content
            if (deltaX < 0) {
                // Swipe left - reveal edit/delete buttons
                const distance = Math.max(deltaX, -SWIPE_REVEAL_WIDTH);
                rowContent.style.transform = `translateX(${distance}px) translateZ(0)`;
                packingRow.classList.add('swiping-left');
                packingRow.classList.remove('swiped-right');
            } else if (deltaX > 0 && packingRow.classList.contains('swiped-left')) {
                // Swipe right to close if already swiped left
                const distance = Math.min(deltaX - SWIPE_REVEAL_WIDTH, 0);
                rowContent.style.transform = `translateX(${distance}px) translateZ(0)`;
            }
        }
    }, { passive: false });

    document.addEventListener('touchend', function(e) {
        const packingRow = e.target.closest('.packing-row');
        if (!packingRow || packingRow.classList.contains('editing')) return;

        const rowContent = packingRow.querySelector('.packing-row-content');
        if (!rowContent) return;

        const touchX = e.changedTouches[0].clientX;
        const deltaX = touchX - touchStartX;

        packingRow.classList.remove('swiping-left');

        if (isSwiping && Math.abs(deltaX) > 50) {
            if (deltaX < 0) {
                // Swiped left - snap to reveal buttons
                rowContent.style.transform = `translateX(-${SWIPE_REVEAL_WIDTH}px) translateZ(0)`;
                packingRow.classList.add('swiped-left');
                currentSwipedItem = packingRow;
            } else if (packingRow.classList.contains('swiped-left')) {
                // Swiped right while open - close
                rowContent.style.transform = '';
                packingRow.classList.remove('swiped-left');
                if (currentSwipedItem === packingRow) {
                    currentSwipedItem = null;
                }
            }
        } else {
            // Snap back
            if (packingRow.classList.contains('swiped-left')) {
                rowContent.style.transform = `translateX(-${SWIPE_REVEAL_WIDTH}px) translateZ(0)`;
            } else {
                rowContent.style.transform = '';
            }
        }

        isSwiping = false;
    });

    // Close swiped item when clicking elsewhere
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.packing-row') && currentSwipedItem) {
            const rowContent = currentSwipedItem.querySelector('.packing-row-content');
            if (rowContent) {
                rowContent.style.transform = '';
            }
            currentSwipedItem.classList.remove('swiped-left', 'swiped-right');
            currentSwipedItem = null;
        }
    });
};

/**
 * Close any swiped items
 */
window.closeSwipedItems = function() {
    const swipedItems = document.querySelectorAll('.packing-row.swiped-left, .packing-row.swiped-right');
    swipedItems.forEach(item => {
        const rowContent = item.querySelector('.packing-row-content');
        if (rowContent) {
            rowContent.style.transform = '';
        }
        item.classList.remove('swiped-left', 'swiped-right');
    });
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
 *      - positions container directly above keyboard with no gap
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

            // Fix container to the viewport and position it directly above the keyboard
            quickAddContainer.style.position = 'fixed';
            quickAddContainer.style.left = '0';
            quickAddContainer.style.right = '0';
            quickAddContainer.style.transform = 'none';
            quickAddContainer.style.top = 'auto';
            
            // Position directly above keyboard with no gap
            if (kbHeight > 0) {
                // Use bottom positioning to place it directly above keyboard
                quickAddContainer.style.bottom = `${kbHeight}px`;
            } else {
                // Fallback: position at bottom of visible viewport
                const containerHeight = quickAddContainer.offsetHeight || 0;
                const visibleHeight = viewportHeight;
                quickAddContainer.style.bottom = 'auto';
                quickAddContainer.style.top = `${visibleHeight - containerHeight}px`;
            }
            
            // Add a small delayed re-calculation to fix initial positioning issue
            // when keyboard first appears and measurements might be unstable
            setTimeout(() => {
                const updatedViewportHeight = window.visualViewport ? window.visualViewport.height : window.innerHeight;
                const updatedKbHeightFromArg = Number(keyboardHeight) || 0;
                const updatedInferredKb = Math.max(0, window.innerHeight - updatedViewportHeight);
                const updatedKbHeight = updatedKbHeightFromArg > 0 ? updatedKbHeightFromArg : updatedInferredKb;
                
                if (updatedKbHeight > 0) {
                    quickAddContainer.style.bottom = `${updatedKbHeight}px`;
                    quickAddContainer.style.top = 'auto';
                } else {
                    const updatedContainerHeight = quickAddContainer.offsetHeight || 0;
                    quickAddContainer.style.bottom = 'auto';
                    quickAddContainer.style.top = `${updatedViewportHeight - updatedContainerHeight}px`;
                }
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

/**
 * Theme Manager
 */
window.themeManager = {
    init: function() {
        const savedTheme = localStorage.getItem('app-theme') || 'light';
        this.applyTheme(savedTheme);
        
        // Listen for system theme changes if auto mode
        if (savedTheme === 'auto') {
            this.watchSystemTheme();
        }
    },
    
    setTheme: function(theme) {
        localStorage.setItem('app-theme', theme);
        this.applyTheme(theme);
        
        if (theme === 'auto') {
            this.watchSystemTheme();
        }
    },
    
    getTheme: function() {
        return localStorage.getItem('app-theme') || 'light';
    },
    
    applyTheme: function(theme) {
        let actualTheme = theme;
        
        if (theme === 'auto') {
            actualTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        
        document.documentElement.setAttribute('data-theme', actualTheme);
    },
    
    watchSystemTheme: function() {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handler = (e) => {
            const currentTheme = localStorage.getItem('app-theme');
            if (currentTheme === 'auto') {
                this.applyTheme('auto');
            }
        };
        
        // Remove old listener if exists
        if (this._themeListener) {
            mediaQuery.removeListener(this._themeListener);
        }
        
        this._themeListener = handler;
        mediaQuery.addListener(handler);
    }
};

// Initialize theme on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => window.themeManager.init());
} else {
    window.themeManager.init();
}