# Localization Best Practices

## Purpose
Localization practices ensure that user-facing text can be translated and adapted without rewriting component code.

## Core Rules
- Treat all user-facing strings as localizable.
- Keep translatable content out of hardcoded markup and logic.
- Use a consistent localization access pattern across the project.
- Update all supported language resources when new text is added.

## Scope of Localization
Localize:
- labels
- button text
- menu items
- validation messages
- error messages
- empty states
- tooltips and titles
- status text and banners

Do not assume internal admin tools or debug screens are exempt unless that is an explicit product decision.

## Content Design Guidance
- Write short, clear source strings.
- Avoid string concatenation that breaks grammar in other languages.
- Prefer full phrases over fragmented token assembly.
- Consider pluralization and variable interpolation early.

## Implementation Guidance
- Use a standard localizer abstraction.
- Keep localization keys stable and descriptive.
- Refresh UI when culture changes if the framework requires it.
- Avoid caching localized text in long-lived state unless invalidation is handled.

## Anti-Patterns
- Hardcoded English text in components.
- Building sentences from many small string fragments.
- Introducing strings in one language resource only.
- Using unstable or unclear localization keys.

## Review Checklist
- [ ] All user-visible strings are localizable.
- [ ] New strings are added to all supported resource sets.
- [ ] String composition is localization-friendly.
- [ ] Culture changes refresh the UI where needed.
