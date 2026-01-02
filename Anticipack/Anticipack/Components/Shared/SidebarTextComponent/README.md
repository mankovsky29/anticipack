# SidebarText Component Usage

## Overview
The `SidebarText` component displays text in the sidebar that can be clicked to edit a packing activity. This component can be controlled from anywhere in the application using the `ISidebarTextService`.

## Features
- Display text in the sidebar next to NavMenu
- Click to edit the associated packing activity in a dialog
- Access from any component via dependency injection

## Usage

### 1. Inject the service in your component:

```razor
@inject ISidebarTextService SidebarTextService
```

### 2. Set the text and packing ID:

```csharp
// Set the text to display
SidebarTextService.SetText("My Packing Activity");

// Set the packing ID that will be edited when clicked
SidebarTextService.SetPackingId(packingActivity.Id);
```

### 3. Example usage in a component:

```razor
@page "/example"
@inject ISidebarTextService SidebarTextService
@inject IPackingRepository PackingRepository

<button @onclick="LoadAndDisplayPacking">Load Packing</button>

@code {
    private async Task LoadAndDisplayPacking()
    {
        var packing = await PackingRepository.GetPackingByIdAsync("some-id");
        if (packing != null)
        {
            // Update the sidebar text
            SidebarTextService.SetText(packing.Name);
            SidebarTextService.SetPackingId(packing.Id);
        }
    }
}
```

## Integration Points

### Where to Update the Sidebar Text
You can update the sidebar text from:
- `PlayPacking.razor` - When a packing activity is loaded for playing
- `EditPacking.razor` - When editing a packing activity
- `PackingActivity.razor` - When viewing packing activities
- Any other component that needs to display current packing information

### Example: Update in PlayPacking.razor

```csharp
protected override async Task OnInitializedAsync()
{
    await LoadPackingActivity();
    
    if (_currentActivity != null)
    {
        // Update sidebar with current activity
        SidebarTextService.SetText(_currentActivity.Name);
        SidebarTextService.SetPackingId(_currentActivity.Id);
    }
}
```

## API Reference

### ISidebarTextService Methods

- `SetText(string text)` - Sets the text to display in the sidebar
- `GetText()` - Gets the current text (returns string)
- `SetPackingId(string? packingId)` - Sets the packing ID for editing

## Styling

The component includes default styling in `SidebarText.razor.css`. You can customize:
- Button appearance
- Hover effects
- Text truncation
- Icon styling

## Notes

- The service is registered as a **Singleton**, so the text persists across the application
- The component shows nothing if no text is set
- Clicking the text opens an edit dialog with the EditPackingForm
- The form auto-saves changes to the repository and updates the sidebar text
