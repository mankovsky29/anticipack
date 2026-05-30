# NavMenu Best Practices

## Purpose
A navigation menu is a shell component that exposes primary routes and core destinations. It should remain simple, predictable, and focused on navigation rather than feature workflows.

## Core Responsibilities
A reusable navigation menu should:
- Render top-level route links.
- Expose a mobile open/close interaction.
- Surface active route state.
- Optionally highlight one primary action.
- Publish shell state changes through a shared contract when needed.

## Architectural Rules
- Keep navigation as a shell-level component.
- Avoid feature business logic inside the menu.
- Use a shared interface or event contract for cross-component coordination.
- Keep local state limited to menu visibility and small UI concerns.

## Link Design
Navigation links should:
- Use route-aware components when available.
- Present icon plus text when that improves scan speed.
- Avoid overloaded card-like layouts for primary navigation.
- Highlight the current route consistently.
- Use one optional emphasized action only when it is genuinely primary.

## State Model
Recommended local state:
- `isCollapsed` or `isExpanded` for mobile overlay behavior
- optional `isAnimating` or transition flags only if they are necessary

Recommended shared state:
- nav expanded/collapsed state for header or shell synchronization

Do not store page form state, filters, or business selection state inside the menu.

## Responsive Behavior
### Mobile
- Show a hamburger or equivalent toggle.
- Hide menu by default if screen space is limited.
- Open the menu as an overlay, drawer, or sheet.
- Close on route selection when appropriate.

### Tablet and Desktop
- Prefer an always-visible sidebar or rail.
- Disable mobile overlay logic when persistent navigation is present.
- Keep widths explicit and stable.

## Styling Guidance
- Use design tokens for all spacing, color, border, radius, and shadow values.
- Keep item height large enough for touch targets.
- Use clear hover, focus, and active states.
- Keep icon sizing consistent across all items.
- Use visual emphasis sparingly.

## Accessibility Guidance
- Toggle button must have an accessible name.
- Decorative icons should be hidden from assistive technology.
- Keyboard users must be able to open, navigate, and close the menu.
- Focus should move logically when the menu opens and closes.
- Motion should be reduced when the user prefers reduced motion.

## Suggested Interaction Flow
1. User activates the menu toggle.
2. Component determines whether mobile overlay behavior applies.
3. Local state changes.
4. CSS class changes the visual state.
5. Optional shell-state event is published.
6. Focus is managed appropriately.

## Anti-Patterns
- Putting feature shortcuts, metrics, badges, and dense controls into every menu item.
- Treating the menu like a dashboard.
- Using hardcoded colors instead of tokenized styles.
- Leaving overlay menus open after route navigation on mobile.
- Ignoring keyboard or screen-reader behavior.

## Review Checklist
- [ ] Menu responsibilities are limited to navigation concerns.
- [ ] Mobile and desktop behaviors are intentionally different.
- [ ] Active states are clear and consistent.
- [ ] Toggle behavior is accessible.
- [ ] Styles are token-based.
- [ ] The menu is not visually overloaded.
