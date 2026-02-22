using Anticipack.Storage;
using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Statistics;

/// <summary>
/// Computes aggregated packing statistics from activity, item, and history data
/// </summary>
public class PackingStatisticsService : IPackingStatisticsService
{
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

    public async Task<PackingStatisticsData> GetStatisticsAsync()
    {
        var activities = await _activityRepository.GetAllAsync();
        var allItems = await _itemRepository.GetAllItemsAsync();
        var allHistory = await _historyRepository.GetAllHistoryAsync();

        var stats = new PackingStatisticsData();

        ComputeOverview(stats, activities, allHistory);
        ComputeTimeStats(stats, activities, allHistory);
        ComputePerActivityStats(stats, activities, allItems, allHistory);
        ComputeUsagePatterns(stats, allHistory);
        ComputeCategoryInsights(stats, allItems);

        return stats;
    }

    private static void ComputeOverview(
        PackingStatisticsData stats,
        List<PackingActivity> activities,
        List<PackingHistoryEntry> history)
    {
        stats.TotalActivities = activities.Count;
        stats.TotalSessions = history.Count;
        stats.TotalItemsPacked = history.Sum(h => h.PackedItems);

        var totalItems = history.Sum(h => h.TotalItems);
        stats.OverallEfficiency = totalItems > 0
            ? Math.Round((double)stats.TotalItemsPacked / totalItems * 100, 1)
            : 0;
    }

    private static void ComputeTimeStats(
        PackingStatisticsData stats,
        List<PackingActivity> activities,
        List<PackingHistoryEntry> history)
    {
        if (history.Count == 0)
            return;

        var validSessions = history.Where(h => h.DurationSeconds > 0).ToList();

        if (validSessions.Count > 0)
        {
            stats.AveragePackingTime = TimeSpan.FromSeconds(validSessions.Average(h => h.DurationSeconds));

            var fastest = validSessions.MinBy(h => h.DurationSeconds)!;
            stats.FastestSession = TimeSpan.FromSeconds(fastest.DurationSeconds);
            stats.FastestSessionActivityName = activities
                .FirstOrDefault(a => a.Id == fastest.ActivityId)?.Name ?? string.Empty;

            stats.TotalTimePacking = TimeSpan.FromSeconds(validSessions.Sum(h => h.DurationSeconds));
        }

        stats.RecentSessionDurations = history
            .Where(h => h.DurationSeconds > 0)
            .OrderByDescending(h => h.CompletedDate)
            .Take(10)
            .OrderBy(h => h.CompletedDate)
            .Select(h => new SessionDurationPoint
            {
                Date = h.CompletedDate,
                DurationSeconds = h.DurationSeconds
            })
            .ToList();
    }

    private static void ComputePerActivityStats(
        PackingStatisticsData stats,
        List<PackingActivity> activities,
        List<PackingItem> allItems,
        List<PackingHistoryEntry> history)
    {
        if (activities.Count == 0)
            return;

        // Most packed activity (most sessions)
        var sessionsByActivity = history
            .GroupBy(h => h.ActivityId)
            .Select(g => new { ActivityId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (sessionsByActivity.Count > 0)
        {
            var top = sessionsByActivity.First();
            var activity = activities.FirstOrDefault(a => a.Id == top.ActivityId);
            if (activity is not null)
            {
                stats.MostPackedActivity = new ActivityStat
                {
                    Name = activity.Name,
                    Value = top.Count
                };
            }
        }

        // Most/least efficient activity (min 2 sessions)
        var efficiencyByActivity = history
            .GroupBy(h => h.ActivityId)
            .Where(g => g.Count() >= 2)
            .Select(g => new
            {
                ActivityId = g.Key,
                Efficiency = g.Sum(h => h.TotalItems) > 0
                    ? Math.Round((double)g.Sum(h => h.PackedItems) / g.Sum(h => h.TotalItems) * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.Efficiency)
            .ToList();

        if (efficiencyByActivity.Count > 0)
        {
            var mostEfficient = efficiencyByActivity.First();
            var mostEfficientActivity = activities.FirstOrDefault(a => a.Id == mostEfficient.ActivityId);
            if (mostEfficientActivity is not null)
            {
                stats.MostEfficientActivity = new ActivityStat
                {
                    Name = mostEfficientActivity.Name,
                    Percentage = mostEfficient.Efficiency
                };
            }

            var leastEfficient = efficiencyByActivity.Last();
            if (leastEfficient.ActivityId != mostEfficient.ActivityId)
            {
                var leastEfficientActivity = activities.FirstOrDefault(a => a.Id == leastEfficient.ActivityId);
                if (leastEfficientActivity is not null)
                {
                    stats.LeastEfficientActivity = new ActivityStat
                    {
                        Name = leastEfficientActivity.Name,
                        Percentage = leastEfficient.Efficiency
                    };
                }
            }
        }

        // Activity with most items
        var itemsByActivity = allItems
            .GroupBy(i => i.ActivityId)
            .Select(g => new { ActivityId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        if (itemsByActivity is not null)
        {
            var activity = activities.FirstOrDefault(a => a.Id == itemsByActivity.ActivityId);
            if (activity is not null)
            {
                stats.MostItemsActivity = new ActivityStat
                {
                    Name = activity.Name,
                    Value = itemsByActivity.Count
                };
            }
        }
    }

    private static void ComputeUsagePatterns(
        PackingStatisticsData stats,
        List<PackingHistoryEntry> history)
    {
        if (history.Count == 0)
            return;

        stats.LastPackedDate = history.Max(h => h.CompletedDate);

        // Sessions this month vs last month
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        stats.SessionsThisMonth = history.Count(h => h.CompletedDate >= startOfMonth);
        stats.SessionsLastMonth = history.Count(h => h.CompletedDate >= startOfLastMonth && h.CompletedDate < startOfMonth);

        // Most active day of week
        var dayGroups = history
            .GroupBy(h => h.CompletedDate.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (dayGroups is not null)
        {
            stats.MostActiveDay = dayGroups.Key;
        }

        // Current streak (consecutive days with sessions)
        var sessionDates = history
            .Select(h => h.CompletedDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (sessionDates.Count > 0)
        {
            int streak = 0;
            var checkDate = DateTime.Today;

            // Allow today or yesterday as streak start
            if (sessionDates[0] == checkDate || sessionDates[0] == checkDate.AddDays(-1))
            {
                checkDate = sessionDates[0];
                foreach (var date in sessionDates)
                {
                    if (date == checkDate)
                    {
                        streak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else if (date < checkDate)
                    {
                        break;
                    }
                }
            }

            stats.CurrentStreak = streak;
        }
    }

    private static void ComputeCategoryInsights(
        PackingStatisticsData stats,
        List<PackingItem> allItems)
    {
        var categoryGroups = allItems
            .Where(i => !string.IsNullOrEmpty(i.Category))
            .GroupBy(i => i.Category)
            .Select(g => new CategoryStat
            {
                Category = g.Key,
                ItemCount = g.Count()
            })
            .OrderByDescending(c => c.ItemCount)
            .ToList();

        stats.CategoryDistribution = categoryGroups;
        stats.MostCommonCategory = categoryGroups.FirstOrDefault()?.Category;
    }
}
