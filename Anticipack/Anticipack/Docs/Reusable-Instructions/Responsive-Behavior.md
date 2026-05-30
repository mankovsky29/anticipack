# Responsive Behavior Best Practices

## Purpose
Responsive behavior should be designed intentionally rather than treated as late-stage CSS cleanup. Layout, navigation, density, and interaction patterns should adapt together.

## Core Principles
- Start with mobile-first defaults.
- Scale up progressively for larger viewports.
- Keep breakpoint rules consistent across related shell components.
- Adapt interaction models, not just widths.

## Breakpoint Strategy
Use a documented breakpoint system. Whether using two, three, or more tiers, define them once and apply them consistently across:
- layout shell
- navigation
- page grids
- spacing and density adjustments

## Responsive Design Rules
- On small screens, prioritize clarity and touch targets.
- On large screens, prioritize scan efficiency and stable layout.
- Avoid changing the meaning of controls across breakpoints.
- Avoid layout jumps caused by inconsistent widths or hidden regions.

## Common Adaptations
### Small screens
- Collapse navigation.
- Increase emphasis on primary actions.
- Stack content vertically.
- Avoid side-by-side forms unless essential.

### Medium and large screens
- Show persistent navigation when useful.
- Use side-by-side content where readability improves.
- Maintain stable widths for side panels.
- Reduce excessive whitespace without making controls cramped.

## Safe-Area Guidance
On mobile devices with cutouts or gesture areas:
- respect safe-area insets
- avoid placing critical controls too close to screen edges
- test overlay and bottom-aligned controls carefully

## Testing Guidance
Validate responsive behavior using:
- narrow phone widths
- common tablet widths
- desktop widths
- orientation changes where applicable

## Anti-Patterns
- Adding random breakpoint overrides without a system.
- Treating desktop and mobile as two unrelated UIs.
- Keeping mobile overlay logic active when desktop sidebar is visible.
- Shrinking touch targets too far on dense layouts.

## Review Checklist
- [ ] Breakpoints are documented and reused consistently.
- [ ] Interaction patterns adapt with layout changes.
- [ ] Safe-area handling is considered.
- [ ] The design remains readable and operable at all target widths.
