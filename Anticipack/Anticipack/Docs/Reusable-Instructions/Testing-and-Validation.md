# Testing and Validation Best Practices

## Purpose
Reusable UI architecture should be validated intentionally. Documentation and conventions are only valuable when teams can verify that implementations follow them.

## Validation Layers
### Review-level validation
Use architecture checklists during pull request review:
- responsibility boundaries are respected
- accessibility rules are followed
- design tokens are used consistently
- responsive rules are coherent across related components

### Unit or logic-level validation
Test:
- state transitions
- service event publication
- conditional visibility logic
- localizable string usage where practical

### Component-level validation
Test:
- rendered structure
- route-aware active states
- conditional classes
- focus or keyboard behavior where test tooling allows it

### UI or end-to-end validation
Test:
- responsive layouts at representative widths
- overlay open and close flows
- keyboard-only usage
- screen-reader-affecting semantics where possible

## Documentation Validation
Before reusing an instruction file in a new project:
- remove stale terminology
- verify the framework features still match the target stack
- confirm naming conventions align with the destination repository
- ensure no project-specific assumptions remain hidden in the text

## Anti-Patterns
- Treating best-practice documents as self-enforcing.
- Skipping accessibility review because visual QA passed.
- Testing only desktop behavior for shell components.
- Reusing guidance verbatim without checking domain fit.

## Review Checklist
- [ ] Architecture rules are enforceable in review.
- [ ] Important state and interaction flows are testable.
- [ ] Responsive and accessibility scenarios are validated.
- [ ] Reused documents are checked for hidden project assumptions.
