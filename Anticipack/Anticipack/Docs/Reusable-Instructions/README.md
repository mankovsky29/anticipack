# Reusable Instruction Files

## Purpose
This folder contains project-independent instruction documents that capture reusable frontend architecture, component design, styling, interaction, and validation practices.

Each file is intentionally focused on one component type or one practice area so it can be copied into another repository, internal handbook, or AI instruction set with minimal editing.

## How to Use
- Copy only the files relevant to the target project.
- Keep the files generic unless a project needs stricter conventions.
- Prefer combining these instructions with local repository rules rather than replacing repository-specific standards.
- Review terminology before reuse so route names, service names, and UI labels match the destination codebase.

## File Index
- `Layout-Shell.md` — shell layout architecture and separation of responsibilities
- `NavMenu.md` — navigation menu design, state, responsiveness, and accessibility
- `Navigation-Header-State.md` — shared shell state and header coordination patterns
- `Design-Tokens-and-Theming.md` — CSS variable strategy, theming, and visual consistency guidance
- `Responsive-Behavior.md` — breakpoint design and viewport-specific layout behavior
- `Accessibility-and-Interaction.md` — semantics, keyboard behavior, motion, and focus management
- `Localization.md` — localization-first UI content practices
- `JavaScript-Interop.md` — when and how to use browser interop safely
- `Component-Structure.md` — component file organization and logic separation
- `Testing-and-Validation.md` — validation criteria for reusable UI architecture

## Recommended Adoption Order
1. Start with `Layout-Shell.md` and `Component-Structure.md`.
2. Add `NavMenu.md` and `Navigation-Header-State.md` for shell navigation behavior.
3. Add `Design-Tokens-and-Theming.md`, `Responsive-Behavior.md`, and `Accessibility-and-Interaction.md` for cross-cutting UI quality.
4. Add `Localization.md` and `JavaScript-Interop.md` where relevant.
5. Use `Testing-and-Validation.md` as a review checklist before rollout.

## Authoring Rules for Future Additions
- Keep each file focused on one practice or one component family.
- Avoid references to repository names, local file paths, or project-specific service names.
- Explain responsibilities, boundaries, lifecycle, edge cases, and anti-patterns.
- Prefer stable principles over framework-version-specific tricks.
- Include checklists where they improve reuse.
