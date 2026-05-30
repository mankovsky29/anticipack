# Component Structure Best Practices

## Purpose
A consistent component structure improves readability, reuse, onboarding, and long-term maintenance.

## Recommended File Pattern
For non-trivial components, use separate files for:
- markup
- component logic
- scoped styles
- optional component-specific JavaScript

Example structure:
- `Component.razor`
- `Component.razor.cs`
- `Component.razor.css`
- `Component.razor.js`

## Responsibility Split
### Markup file
- route directive if needed
- HTML and component composition
- no large logic blocks

### Code-behind file
- dependency injection
- state
- lifecycle methods
- event handlers
- async operations

### Style file
- scoped component styles
- token-based values only
- responsive and state-specific presentation rules

### JavaScript file
- only DOM or browser-specific behavior
- exported functions with narrow scope

## Dependency Injection Guidance
- Inject only what the component actually uses.
- Prefer interfaces over concrete implementations.
- Keep component dependencies cohesive.

## State Guidance
- Keep UI state local unless it must be shared.
- Extract repeated business operations into services.
- Use clear private field naming conventions.
- Keep async methods consistently suffixed.

## Anti-Patterns
- Large inline code sections inside markup files.
- Mixing rendering, business logic, styling, and browser integration in one place.
- Injecting too many unrelated services.
- Repeating the same logic across multiple components instead of extracting services.

## Review Checklist
- [ ] Markup, logic, style, and browser-specific code are separated.
- [ ] Dependencies are minimal and interface-driven.
- [ ] UI state is local unless shared state is justified.
- [ ] Complex business logic lives outside the component.
