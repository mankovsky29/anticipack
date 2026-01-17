# NavigationHeader Component

## Overview
The NavigationHeader component is a flexible navigation bar element that displays contextual text and a donate button. It's responsive and adapts to different screen sizes, working in conjunction with the NavMenu state.

## Features
- **Dynamic Text Display**: Shows contextual information about the current page or active item
- **Donate Button**: Always-visible gift icon button that navigates to the donation page
- **Responsive Behavior**:
  - **Mobile**: Donate button is visible only when NavMenu is collapsed
  - **Tablet & Desktop**: Donate button is always visible
- **Event-Driven Architecture**: Uses the NavigationHeaderService for state management

## Usage

### In a Page Component

```csharp
@inject INavigationHeaderService NavigationHeaderService

@code {
    protected override void OnInitialized()
    {
        // Set the header text for this page
        NavigationHeaderService.SetText("My Page Title");
    }
}
```

### Service Registration

The service is registered in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<INavigationHeaderService, NavigationHeaderService>();
```

## NavigationHeaderService API

### Methods

- `SetText(string text)` - Sets the text to display in the navigation header
- `GetText()` - Returns the current text
- `SetNavMenuExpanded(bool isExpanded)` - Updates the NavMenu state (used internally by NavMenu)
- `IsNavMenuExpanded()` - Returns the current NavMenu state

### Events

- `OnTextChanged` - Fired when the header text changes
- `OnNavMenuToggled` - Fired when the NavMenu state changes

## Styling

The component uses CSS custom properties for theming and includes responsive breakpoints at:
- Mobile: < 641px
- Tablet: 641px - 1023px
- Desktop: >= 1024px

## Related Components

- **NavMenu**: Contains the NavigationHeader and manages the navigation state
- **DonatePage**: The destination page when the donate button is clicked
