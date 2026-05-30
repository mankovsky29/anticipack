# Accessibility and Interaction Best Practices

## Purpose
Accessibility should be a built-in quality standard for all UI components, not an optional enhancement added at the end.

## Core Principles
- Every interactive element must be reachable and understandable.
- Visual-only cues should not be the only way to understand state.
- Keyboard, screen-reader, and reduced-motion users must be considered during design.

## Semantics
- Use native HTML elements whenever possible.
- Use buttons for actions and links for navigation.
- Add accessible names to controls that rely on icons.
- Mark decorative icons as hidden from assistive technology.
- Use landmark regions where they improve navigation.

## Keyboard Behavior
- All interactive elements must be reachable by keyboard.
- Focus order should follow the visual and logical reading order.
- Overlays, drawers, and menus should support keyboard close behavior such as `Escape`.
- Focus should move intentionally into transient UI and return appropriately when it closes.

## Motion and Contrast
- Honor `prefers-reduced-motion` for non-essential animations.
- Provide visible focus states.
- Ensure text and controls preserve sufficient contrast.
- Add high-contrast support where the design system requires it.

## Status and Feedback
- Use suitable live-region patterns for important dynamic updates.
- Do not rely only on color to signal success, warning, or error states.
- Keep feedback short, timely, and contextual.

## Touch and Pointer Guidance
- Ensure touch targets are large enough.
- Avoid tightly packed controls in navigation or shell-level chrome.
- Provide hover enhancements only as an addition, never as the sole interaction mode.

## Anti-Patterns
- Icon-only controls without names.
- Clickable non-semantic containers used instead of buttons or links.
- Menus that open visually but do not move focus.
- Animations that cannot be reduced or disabled.
- Error states that depend only on red color.

## Review Checklist
- [ ] All controls have accessible names.
- [ ] Keyboard navigation works end to end.
- [ ] Focus handling is intentional.
- [ ] Motion and contrast preferences are respected.
- [ ] Dynamic feedback is announced appropriately.
