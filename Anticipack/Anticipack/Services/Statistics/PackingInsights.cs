using Anticipack.Storage;

namespace Anticipack.Services.Statistics;

/// <summary>
/// A light personal summary plus the lists that need the user's attention
/// </summary>
public class PackingInsights
{
    // Summary
    public bool HasHistory { get; set; }
    public int TripsThisYear { get; set; }
    public TimeSpan PackingTimeThisYear { get; set; }
    public PackingActivity? GoToList { get; set; }
    public int GoToListTrips { get; set; }

    // List health
    public List<UnfinishedList> UnfinishedLists { get; set; } = [];
    public List<PackingActivity> UnusedLists { get; set; } = [];
    public List<PackingActivity> EmptyLists { get; set; } = [];
    public List<SimilarLists> SimilarLists { get; set; } = [];

    public bool HasListHealthIssues =>
        UnfinishedLists.Count > 0 || UnusedLists.Count > 0 || EmptyLists.Count > 0 || SimilarLists.Count > 0;
}

public record UnfinishedList(PackingActivity Activity, int PackedItems, int TotalItems);

public record SimilarLists(PackingActivity First, PackingActivity Second, int SharedPercent);
