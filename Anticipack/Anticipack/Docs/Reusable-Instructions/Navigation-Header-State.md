# Navigation Header State Best Practices

## Purpose
A shared shell-state contract allows navigation, header, and other layout components to coordinate without direct component references.

This pattern is useful when multiple shell components need to react to:
- current section title
- nav expanded or collapsed state
- shell-level mode changes

## Why Use a Shared Contract
Benefits:
- loose coupling between layout components
- simpler unit testing through interface mocking
- clearer separation between state ownership and state presentation
- easier future refactoring of shell components

## Recommended Contract Design
A generic shell-state interface should expose:
- event for title changes
- event for nav visibility changes
- getter for current title
- setter for current title
- getter for nav state
- setter for nav state

Optionally include additional shell signals only if they are truly shell-wide.

## Design Rules
- Keep method names generic and intent-based.
- Keep event payloads small and predictable.
- Do not include page business entities in the contract.
- Do not let the service grow into an application-wide state bag.

## Ownership Rules
- The page or feature decides what title should be shown.
- The header decides how to render the title.
- The nav menu decides when the menu is open or closed.
- The shared contract only transports state changes.

## Lifetime Guidance
The shell-state service is usually long-lived within the UI session. Choose the lifetime that matches how shell state should persist during navigation.

## Threading and Async Considerations
- Keep setters lightweight.
- Fire events predictably.
- Avoid heavy async work inside event handlers.
- If state changes may originate from async callbacks, ensure UI updates are marshaled safely to the UI thread when required by the framework.

## Anti-Patterns
- Directly calling methods on sibling components.
- Storing feature data collections in the shell-state service.
- Adding dozens of unrelated properties because the service is convenient.
- Making every page aware of every shell detail.

## Review Checklist
- [ ] The contract is small and focused.
- [ ] Components communicate through the interface, not direct references.
- [ ] Title state and nav state have clear owners.
- [ ] The service is not misused as a general application store.
