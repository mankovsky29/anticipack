namespace Anticipack.API.Models;

public class UserSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    
    // Notification settings
    public bool EnableNotifications { get; set; } = true;
    public bool EnableEmailNotifications { get; set; } = false;
    public bool EnablePushNotifications { get; set; } = true;
    
    // App preferences
    public string Theme { get; set; } = "System"; // System, Light, Dark
    public string DefaultCategory { get; set; } = "General";
    public bool AutoResetPackedItems { get; set; } = true;
    public int ReminderHoursBeforePacking { get; set; } = 24;
    
    // Privacy settings
    public bool AllowDataCollection { get; set; } = true;
    public bool ShareAnonymousUsage { get; set; } = true;
    
    // Display preferences
    public string Language { get; set; } = "en";
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public bool ShowCompletedActivities { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User? User { get; set; }
}
