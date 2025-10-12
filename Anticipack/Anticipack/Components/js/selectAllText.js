export function selectAllText(el) {
  if (!el) return;
  if (typeof el.select === "function") {
    el.focus();
    try {
      el.select();
      if (typeof el.setSelectionRange === "function") {
        const len = (el.value ?? "").length;
        el.setSelectionRange(0, len);
      }
    } catch { /* ignore */ }
    return;
  }
  // Fallback for non-inputs
  const range = document.createRange();
  range.selectNodeContents(el);
  const sel = window.getSelection();
  sel.removeAllRanges();
  sel.addRange(range);
}