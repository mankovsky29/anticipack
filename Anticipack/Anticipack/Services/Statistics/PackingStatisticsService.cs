using Anticipack.Storage;
using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Statistics;

/// <summary>
/// Computes the personal packing summary and list health insights from activity, item, and history data
/// </summary>
public class PackingStatisticsService : IPackingStatisticsService
{
    private const int UnusedAfterDays = 90;
    private const int GoToListMinTrips = 2;
    private const int SimilarityMinItems = 3;
    private const double SimilarityThreshold = 0.8;

    private readonly IPackingActivityRepository _activityRepository;
    private readonly IPackingItemRepository _itemRepository;
    private readonly IPackingHistoryRepository _historyRepository;

    public PackingStatisticsService(
        IPackingActivityRepository activityRepository,
        IPackingItemRepository itemRepository,
        IPackingHistoryRepository historyRepository)
    {
        _activityRepository = activityRepository;
        _itemRepository = itemRepository;
        _historyRepository = historyRepository;
    }

    public async Task<PackingInsights> GetInsightsAsync()
    {
        var activities = await _activityRepository.GetAllAsync();
        var allItems = await _itemRepository.GetAllItemsAsync();
        var allHistory = await _historyRepository.GetAllHistoryAsync();

        var itemsByActivity = allItems
            .GroupBy(i => i.ActivityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Archived lists are out of the user's way already, so they never need attention
        var activeLists = activities
            .Where(a => !a.IsArchived)
            .Select(a => (Activity: a, Items: itemsByActivity.GetValueOrDefault(a.Id) ?? []))
            .ToList();

        var insights = new PackingInsights();
        ComputeSummary(insights, activities, allHistory);
        ComputeListHealth(insights, activeLists);
        return insights;
    }

    private static void ComputeSummary(
        PackingInsights insights,
        List<PackingActivity> activities,
        List<PackingHistoryEntry> history)
    {
        var now = DateTime.Now;
        var startOfYear = new DateTime(now.Year, 1, 1);
        var thisYear = history.Where(h => h.CompletedDate >= startOfYear).ToList();

        insights.HasHistory = history.Count > 0;
        insights.TripsThisYear = thisYear.Count;

        // Only measured sessions count; older ones recorded screen time, not packing time
        insights.PackingTimeThisYear = TimeSpan.FromSeconds(thisYear.Sum(h => h.ActiveDurationSeconds ?? 0));

        var goTo = history
            .Where(h => h.CompletedDate >= now.AddYears(-1))
            .GroupBy(h => h.ActivityId)
            .Select(g => (Activity: activities.FirstOrDefault(a => a.Id == g.Key), Trips: g.Count()))
            .Where(x => x.Activity is not null && x.Trips >= GoToListMinTrips)
            .OrderByDescending(x => x.Trips)
            .FirstOrDefault();

        insights.GoToList = goTo.Activity;
        insights.GoToListTrips = goTo.Trips;
    }

    private static void ComputeListHealth(
        PackingInsights insights,
        List<(PackingActivity Activity, List<PackingItem> Items)> lists)
    {
        // Packing was started (some items checked) but never completed
        insights.UnfinishedLists = lists
            .Where(l => !l.Activity.IsFinished)
            .Select(l => new UnfinishedList(l.Activity, l.Items.Count(i => i.IsPacked), l.Items.Count))
            .Where(u => u.PackedItems > 0 && u.PackedItems < u.TotalItems)
            .OrderByDescending(u => u.Activity.SessionLastActivityAt ?? u.Activity.LastPacked)
            .ToList();

        var unfinishedIds = insights.UnfinishedLists.Select(u => u.Activity.Id).ToHashSet();
        var unusedCutoff = DateTime.Now.AddDays(-UnusedAfterDays);

        insights.UnusedLists = lists
            .Where(l => l.Items.Count > 0
                && l.Activity.LastPacked < unusedCutoff
                && !unfinishedIds.Contains(l.Activity.Id))
            .Select(l => l.Activity)
            .OrderBy(a => a.LastPacked)
            .ToList();

        insights.EmptyLists = lists
            .Where(l => l.Items.Count == 0)
            .Select(l => l.Activity)
            .ToList();

        insights.SimilarLists = FindSimilarLists(lists);
    }

    /// <summary>
    /// Pairs of lists whose item names mostly overlap (Jaccard similarity), likely duplicates
    /// </summary>
    private static List<SimilarLists> FindSimilarLists(List<(PackingActivity Activity, List<PackingItem> Items)> lists)
    {
        var candidates = lists
            .Where(l => l.Items.Count >= SimilarityMinItems)
            .Select(l => (l.Activity, Names: l.Items
                .Select(i => i.Name.Trim().ToLowerInvariant())
                .ToHashSet()))
            .ToList();

        var pairs = new List<SimilarLists>();

        for (var i = 0; i < candidates.Count; i++)
        {
            for (var j = i + 1; j < candidates.Count; j++)
            {
                var first = candidates[i].Names;
                var second = candidates[j].Names;

                var shared = first.Count(second.Contains);
                var union = first.Count + second.Count - shared;
                var similarity = (double)shared / union;

                if (similarity >= SimilarityThreshold)
                {
                    pairs.Add(new SimilarLists(candidates[i].Activity, candidates[j].Activity, (int)Math.Round(similarity * 100)));
                }
            }
        }

        return pairs.OrderByDescending(p => p.SharedPercent).ToList();
    }
}
