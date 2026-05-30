# Layout Shell Best Practices

## Purpose
A layout shell defines the structural frame of an application. It hosts persistent UI regions such as navigation, header, content area, banners, and shell-level overlays.

A good shell keeps routing, feature logic, and persistent chrome clearly separated.

## Core Responsibilities
The layout shell should:
- Place persistent navigation in a dedicated region.
- Provide a main content region for routed pages.
- Host shell-level notifications, banners, dialogs, or toasts when appropriate.
- Coordinate responsive transitions between mobile and larger layouts.
- Avoid feature-specific business logic.

## Recommended Regions
A reusable shell commonly includes:
- `sidebar` or navigation rail
- `top header` or app bar
- `main content`
- optional `status banner` region
- optional `global overlay` region

Not every project needs all regions, but the shell should make those responsibilities explicit.

## Structural Principles

### Keep shell concerns separate from feature concerns
Feature pages should not own shell behavior such as global navigation visibility, app-wide banners, or persistent header state.

### Compose, do not overload
The shell should compose reusable components such as:
- navigation menu
- header/title component
- toast host
- modal host

It should not become a dumping ground for unrelated code.

### Keep layout state small
Shell state should be limited to concerns such as:
- current header text
- nav open/closed state
- online/offline banner state
- permission or system-level warning state

Avoid moving page-specific state into the shell.

## Responsive Shell Strategy
Use a mobile-first layout:
- Narrow viewports: stacked layout with collapsible navigation.
- Wide viewports: sidebar plus content in a horizontal arrangement.

The shell and its child navigation component must share the same breakpoints. Mismatched breakpoints create broken transitions and duplicated navigation behaviors.

## Recommended Layout Rules
- The root shell container should fill the viewport.
- The main content region should flex to consume remaining space.
- The sidebar should have explicit width on larger breakpoints.
- Sticky or fixed regions should be used intentionally and tested with long content.
- Safe-area insets should be respected for mobile devices.

## Styling Guidance
- Use design tokens for spacing, colors, shadows, and radii.
- Avoid hardcoded layout values unless they are part of a documented scale.
- Keep shell background and content background intentionally distinct when visual hierarchy matters.
- Use subtle borders or shadows to separate navigation from content.

## Accessibility Guidance
- Landmark roles should be used where appropriate.
- Persistent navigation should be reachable by keyboard and screen readers.
- Shell banners should use suitable live-region behavior when content changes dynamically.
- Any modal or toast hosted at shell level must preserve focus and announcement behavior.

## Anti-Patterns
- Embedding feature page logic in the shell.
- Using unrelated shell regions to store page-specific actions.
- Letting layout and navigation define different breakpoint systems.
- Hardcoding pixel values everywhere without a spacing scale.
- Allowing shell overlays to trap users without keyboard escape behavior.

## Review Checklist
- [ ] The layout shell owns only shell concerns.
- [ ] Navigation, header, and content regions are clearly separated.
- [ ] Breakpoints are shared with navigation behavior.
- [ ] Safe-area handling is considered.
- [ ] Global banners and overlays are intentional and accessible.
- [ ] Styling uses design tokens instead of scattered magic values.
