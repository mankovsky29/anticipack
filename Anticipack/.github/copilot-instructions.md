# Copilot Instructions

## Project Guidelines
- In `EditPacking.razor`, clicking outside `add-item-form` should cancel (not auto-add). Clicking outside `item-edit-container` should auto-save changes including notes.
- JS code related to a specific component or form should be placed in a co-located `.razor.js` file next to the Razor page it belongs to (Blazor JS isolation pattern). Only shared/global JS functionality should go in `site.js`.