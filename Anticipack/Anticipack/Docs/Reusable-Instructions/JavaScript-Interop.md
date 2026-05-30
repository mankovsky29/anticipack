# JavaScript Interop Best Practices

## Purpose
JavaScript interop should be used only where browser APIs or DOM behavior cannot be handled reliably with component code and CSS alone.

## Good Use Cases
- viewport or media-query checks not conveniently available in managed code
- focus management helpers
- document-level event listeners
- resize and orientation listeners
- browser-only measurements and scrolling coordination

## Core Rules
- Keep interop APIs small and purpose-specific.
- Prefer co-located modules for component-specific behavior.
- Dispose event listeners and module references during component cleanup.
- Pass all required parameters explicitly; do not rely on hidden defaults.

## Design Guidance
- Use managed code for application logic.
- Use JavaScript for DOM integration boundaries.
- Avoid duplicating the same rules in both JavaScript and component logic.
- Keep naming stable and descriptive.

## Lifecycle Guidance
- Initialize modules only when the DOM is ready.
- Register listeners after render if required by the framework.
- Unregister listeners during disposal.
- Protect against repeated initialization.

## Error and Reliability Guidance
- Handle missing elements safely.
- Prefer idempotent registration patterns where possible.
- Keep event handlers narrow and efficient.
- Test resize and orientation changes, not just first render.

## Anti-Patterns
- Moving business logic into JavaScript.
- Creating large global scripts for component-specific behavior.
- Forgetting to unregister document-level listeners.
- Using interop for styling that CSS already solves well.

## Review Checklist
- [ ] Interop is used only where necessary.
- [ ] Component-specific logic lives in component-specific modules.
- [ ] Listeners and module references are disposed correctly.
- [ ] API parameters are explicit and documented.
