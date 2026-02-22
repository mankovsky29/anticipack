namespace Anticipack.Services.Statistics;

/// <summary>
/// Aggregated packing statistics across all activities
/// </summary>
public class PackingStatisticsData
{
    // Overview
    public int TotalActivities { get; set; }
    public int TotalSessions { get; set; }
    public int TotalItemsPacked { get; set; }
    public double OverallEfficiency { get; set; }

    // Time Stats
    public TimeSpan AveragePackingTime { get; set; }
    public TimeSpan FastestSession { get; set; }
    public string FastestSessionActivityName { get; set; } = string.Empty;
    public TimeSpan TotalTimePacking { get; set; }
    public List<SessionDurationPoint> RecentSessionDurations { get; set; } = [];

    // Per-Activity
    public ActivityStat? MostPackedActivity { get; set; }
    public ActivityStat? MostEfficientActivity { get; set; }
    public ActivityStat? LeastEfficientActivity { get; set; }
    public ActivityStat? MostItemsActivity { get; set; }

    // Usage Patterns
    public int CurrentStreak { get; set; }
    public int SessionsThisMonth { get; set; }
    public int SessionsLastMonth { get; set; }
    public DayOfWeek? MostActiveDay { get; set; }
    public DateTime? LastPackedDate { get; set; }

    // Category Insights
    public List<CategoryStat> CategoryDistribution { get; set; } = [];
    public string? MostCommonCategory { get; set; }
}

public class ActivityStat
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public double Percentage { get; set; }
}

public class CategoryStat
{
    public string Category { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class SessionDurationPoint
{
    public DateTime Date { get; set; }
    public int DurationSeconds { get; set; }
}
