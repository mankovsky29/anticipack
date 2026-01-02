# Integration Example

## How to integrate SidebarText in PlayPacking.razor

Add this to your PlayPacking.razor to automatically update the sidebar when loading a packing activity:

```csharp
@inject ISidebarTextService SidebarTextService

// In your code section, after loading the packing activity:
protected override async Task OnInitializedAsync()
{
    await LoadPackingActivity();
    
    if (_currentActivity != null)
    {
        // Update the sidebar with the current activity name and ID
        SidebarTextService.SetText(_currentActivity.Name);
        SidebarTextService.SetPackingId(_currentActivity.Id);
    }
}

// Also update when the activity name changes:
private void OnActivityNameChanged()
{
    if (_currentActivity != null)
    {
        SidebarTextService.SetText(_currentActivity.Name);
    }
}
```

## How to integrate in PackingActivity.razor

```csharp
@inject ISidebarTextService SidebarTextService

// When a user selects a packing activity:
private void OnPackingSelected(PackingActivity packing)
{
    SidebarTextService.SetText(packing.Name);
    SidebarTextService.SetPackingId(packing.Id);
    
    // Navigate or do other actions
}
```

## How to integrate in EditPacking.razor

```csharp
@inject ISidebarTextService SidebarTextService

// After loading the packing for editing:
protected override async Task OnInitializedAsync()
{
    // ... load packing logic
    
    if (_currentActivity != null)
    {
        SidebarTextService.SetText(_currentActivity.Name);
        SidebarTextService.SetPackingId(_currentActivity.Id);
    }
}

// Update when the name is changed:
private void OnNameChanged()
{
    if (_currentActivity != null && !string.IsNullOrWhiteSpace(_currentActivity.Name))
    {
        SidebarTextService.SetText(_currentActivity.Name);
    }
}
```

## Clearing the Sidebar Text

When navigating away or when no activity is selected:

```csharp
// Clear the sidebar text
SidebarTextService.SetText(string.Empty);
SidebarTextService.SetPackingId(null);
```
