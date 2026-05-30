# Design Tokens and Theming Best Practices

## Purpose
Design tokens create a shared visual language for components. They reduce inconsistency and make theme changes manageable.

## Token Categories
Define tokens for:
- color palette
- semantic surface colors
- text colors
- spacing scale
- typography scale
- radii
- shadows
- transitions
- z-index layers when needed
- safe-area insets for device-specific padding

## Core Rules
- Prefer semantic tokens over raw palette values in component styles.
- Use consistent naming for scale-based values.
- Keep tokens global and component styles local.
- Add dark-theme or alternate-theme token values at the theme layer, not inside each component.

## Semantic Token Guidance
Examples of useful semantics:
- primary action color
- danger color
- success color
- background and surface colors
- border color
- muted text color
- hover and focus backgrounds

Semantic naming allows the underlying palette to change without editing every component.

## Component Styling Rules
- Component styles should consume tokens only.
- Avoid hardcoded hex values, ad hoc spacing, or one-off shadows unless formally added to the token set.
- If a new visual value is repeatedly needed, promote it to a token.

## Theme Support
At minimum consider:
- light theme
- dark theme
- high-contrast needs where applicable

Each theme should redefine token values while keeping token names stable.

## Token Governance
Before adding a new token:
- verify an equivalent token does not already exist
- choose semantic naming over context-specific naming
- define both default and alternate theme values when needed
- document intended usage

## Anti-Patterns
- Token names tied to one page or one component.
- Multiple nearly identical spacing values without a scale.
- Direct color literals scattered across component files.
- Theme overrides written per component instead of through tokens.

## Review Checklist
- [ ] Components use semantic tokens instead of literals.
- [ ] Theme values are centralized.
- [ ] New tokens are named for intent, not one screen.
- [ ] Spacing, radius, and shadow values come from a scale.
