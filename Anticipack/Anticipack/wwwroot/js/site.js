function selectAllText(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.select();
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
