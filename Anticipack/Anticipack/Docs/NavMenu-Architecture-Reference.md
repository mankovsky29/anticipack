# NavMenu Architecture & Style Reference (Project-Independent)

## 1) What NavMenu Is

`NavMenu` is a shell-level navigation component for apps using Blazor (including MAUI Blazor Hybrid, Server, or WebAssembly).

Its goals:
- Provide primary route navigation.
- Support mobile-friendly open/close behavior.
- Integrate with the app shell header through a decoupled state contract.

`NavMenu` belongs to the layout layer, not to feature pages.

---

## 2) Recommended Folder and File Structure

Use a code-behind pattern for maintainability:

- `Layout/NavMenu.razor` — markup only
- `Layout/NavMenu.razor.cs` — logic/state/injections
- `Layout/NavMenu.razor.css` — scoped styles
- `Layout/MainLayout.razor` — host layout
- `Layout/MainLayout.razor.css` — host layout styles
- `Shared/Navigation/INavigationShellState.cs` — contract for shell state
- `Shared/Navigation/NavigationShellState.cs` — implementation

---

## 3) Architectural Principles

### A. Separation of concerns
- `MainLayout` composes shell regions (sidebar, header, main content).
- `NavMenu` renders links and owns local menu state.
- Header component renders title/status independently.

### B. Decoupled communication
- `NavMenu` should never directly modify another component.
- Use a shared state service (interface + implementation) with events.

### C. Route-native navigation
- Use `NavLink` for active-state highlighting and route transitions.

### D. Localization-ready by default
- All user-facing strings should come from a localization provider.

### E. Mobile-first responsive design
- Base behavior targets narrow screens first.
- Wider breakpoints progressively switch to persistent sidebar mode.

---

## 4) Component Responsibilities

## NavMenu responsibilities
- Render top-level navigational links.
- Render one optional quick-action link.
- Toggle overlay menu on mobile only.
- Raise menu-expanded state changes through the shell-state service.

## MainLayout responsibilities
- Place `NavMenu` in a sidebar region.
- Render page content in a main region.
- Keep layout breakpoints aligned with `NavMenu` breakpoints.

## Header responsibilities
- Display current shell title/section context.
- React to shell-state events (e.g., menu opened/closed).

---

## 5) State and Event Contract (Portable Pattern)

Define a shell state interface with event-driven updates.

Suggested responsibilities:
- Header text state (`SetText`, `GetText`, event on change)
- Nav expansion state (`SetNavExpanded`, `IsNavExpanded`, event on change)

This gives:
- Loose coupling
- Easier testing (mock interface)
- Predictable state transitions

---

## 6) Interaction Flow

Typical mobile toggle flow:

1. User taps hamburger button.
2. Component checks if current viewport is mobile.
3. If mobile, toggle boolean state.
4. Computed CSS class switches between collapsed/expanded menu views.
5. Menu state event is published via shell-state service.

Desktop/tablet should ignore overlay toggle and keep sidebar visible.

---

## 7) Responsive Strategy

Recommended breakpoints (customize to your design system):
- Mobile: `< 641px`
- Tablet: `641px–1023px`
- Desktop: `>= 1024px`

Behavior by breakpoint:

### Mobile
- Top row with hamburger visible.
- Menu hidden when collapsed.
- Menu displayed as full-screen overlay when expanded.

### Tablet/Desktop
- Top row can be hidden (if persistent sidebar is used).
- Sidebar always visible.
- Overlay behavior disabled.

Key rule: `MainLayout` and `NavMenu` must share the same breakpoint logic.

---

## 8) Styling System (Design Tokens First)

Use CSS custom properties as design tokens. Avoid hardcoded values in component styles.

Token groups to define globally:
- Colors: primary/surface/background/text/border/hover states
- Typography: font family and size scale
- Spacing scale
- Border radius scale
- Shadow scale
- Transition durations/easings
- Safe-area insets (for mobile devices with notches)

Benefits:
- Easy theming (light/dark/high-contrast)
- Consistent spacing/visual rhythm
- Lower refactor cost

---

## 9) Accessibility and UX Baseline

Minimum requirements:
- Menu toggle button has both `title` and `aria-label`.
- Decorative icons use `aria-hidden="true"`.
- Keyboard support: `Escape` closes overlay menu.
- Focus management: move focus into menu when opened and restore on close.
- Add `prefers-reduced-motion` handling for animations.
- Add `prefers-contrast: high` enhancements.

Optional improvements:
- Trap focus within mobile overlay when open.
- Add screen-reader announcement for open/close state changes.

---

## 10) JavaScript Interop Guidance

Use JS interop only for browser-specific behavior that CSS cannot robustly solve.

Good candidates:
- Viewport checks (`isMobileViewport`).
- Resize listeners for runtime breakpoint recalculation.
- Focus management helpers.

Best practices:
- Keep interop API small and stable.
- Dispose listeners on component disposal.
- Avoid logic duplication between C# and JS.

---

## 11) Performance Guidance

- Keep nav render tree shallow (small number of elements).
- Avoid unnecessary re-renders (only update when nav state changes).
- Keep icon strategy lightweight (SVG/icon font consistency).
- Avoid expensive animations; use transform/opacity if animated.

---

## 12) Security and Reliability Notes

- Do not expose protected routes as visible links unless authorization state is considered.
- Prefer route guards/authorization policies in addition to hiding links.
- Keep shell-state service thread-safe if used across async callbacks.

---

## 13) Testing Strategy

### Unit tests
- Nav state toggles correctly on mobile.
- Toggle ignored on desktop/tablet mode.
- State service events fire with expected payloads.

### Component tests
- Active `NavLink` styling reflects current route.
- Overlay classes switch correctly.
- Localized labels are rendered.

### UI/E2E tests
- Mobile menu opens/closes via touch/click and keyboard.
- Responsive transitions across breakpoints.
- Accessibility smoke checks (labels, focus order).

---

## 14) Reference Implementation Checklist

- [ ] `NavMenu` is layout-level, not feature-level.
- [ ] Markup, logic, and CSS are split (`.razor`, `.razor.cs`, `.razor.css`).
- [ ] All labels are localizable.
- [ ] `NavLink` is used for route items.
- [ ] Mobile overlay + desktop persistent sidebar are both implemented.
- [ ] Breakpoints are consistent with `MainLayout`.
- [ ] CSS uses design tokens only.
- [ ] Safe-area insets are supported.
- [ ] `aria-label`, keyboard close, and reduced-motion support are implemented.
- [ ] Shell-state service contract is interface-driven and event-based.

---

## 15) Anti-Patterns to Avoid

- Embedding feature business logic in `NavMenu`.
- Hardcoded colors/spacing directly in component CSS.
- Direct component-to-component references for shell sync.
- Overlay-only menu on desktop widths.
- No keyboard accessibility for the mobile menu.

---

## 16) Suggested Adaptation Notes for Any New Project

When reusing this architecture:
- Rename routes and menu groups to your domain.
- Keep service contract names generic (`INavigationShellState`).
- Replace icon pack freely (Font Awesome, Fluent, SVG sprite), but keep semantics.
- Keep the same separation model and responsiveness rules even if visual design changes.
