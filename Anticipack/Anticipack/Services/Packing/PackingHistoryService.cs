using Anticipack.Storage;
using Anticipack.Storage.Repositories;

namespace Anticipack.Services.Packing
{
    public class PackingHistoryService : IPackingHistoryService
    {
        private readonly IPackingHistoryRepository _historyRepository;

        public PackingHistoryService(IPackingHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        // Only the most recent sessions matter: older habits may no longer apply
        private const int SkipInsightSessionWindow = 5;
        private const int SkipInsightMinSkips = 2;
        private const int TypicalTimeSessionWindow = 5;

        public async Task RecordPackingSessionAsync(string activityId, DateTime startTime, DateTime endTime, TimeSpan activeDuration, int totalItems, int packedItems, IReadOnlyCollection<string>? skippedItemIds = null)
        {
            var entry = new PackingHistoryEntry
            {
                ActivityId = activityId,
                StartTime = startTime,
                EndTime = endTime,
                CompletedDate = endTime,
                TotalItems = totalItems,
                PackedItems = packedItems,
                DurationSeconds = (int)(endTime - startTime).TotalSeconds,
                ActiveDurationSeconds = (int)activeDuration.TotalSeconds,
                SkippedItemIds = string.Join(',', skippedItemIds ?? [])
            };

            await _historyRepository.AddHistoryEntryAsync(entry);
        }

        public async Task<List<SkippedItemInsight>> GetOftenSkippedItemsAsync(string activityId, IEnumerable<PackingItem> items)
        {
            // Sessions recorded before skip tracking have null SkippedItemIds and say nothing about skips
            var sessions = (await _historyRepository.GetHistoryForActivityAsync(activityId))
                .Where(h => h.SkippedItemIds is not null)
                .Take(SkipInsightSessionWindow)
                .Select(h => (h.CompletedDate, Skipped: h.SkippedItemIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet()))
                .ToList();

            var insights = new List<SkippedItemInsight>();

            foreach (var item in items)
            {
                // Once the user chose to keep an item, only skips after that decision count
                var relevant = sessions
                    .Where(s => item.SkipSuggestionDismissedAt is null || s.CompletedDate > item.SkipSuggestionDismissedAt)
                    .ToList();

                var skippedCount = relevant.Count(s => s.Skipped.Contains(item.Id));

                // Skipped repeatedly and in at least half of the sessions
                if (skippedCount >= SkipInsightMinSkips && skippedCount * 2 >= relevant.Count)
                {
                    insights.Add(new SkippedItemInsight(item, skippedCount, relevant.Count));
                }
            }

            return insights
                .OrderByDescending(i => (double)i.SkippedCount / i.SessionCount)
                .ToList();
        }

        public async Task<TimeSpan?> GetTypicalPackingTimeAsync(string activityId)
        {
            var history = await _historyRepository.GetHistoryForActivityAsync(activityId);
            return GetTypicalTime(history);
        }

        public async Task<Dictionary<string, TimeSpan>> GetTypicalPackingTimesAsync()
        {
            var history = await _historyRepository.GetAllHistoryAsync();

            return history
                .GroupBy(h => h.ActivityId)
                .Select(g => (ActivityId: g.Key, Typical: GetTypicalTime(g)))
                .Where(x => x.Typical.HasValue)
                .ToDictionary(x => x.ActivityId, x => x.Typical!.Value);
        }

        /// <summary>
        /// Median active time of the most recent measured sessions; the median keeps
        /// one unusually slow or fast session from distorting the estimate.
        /// </summary>
        private static TimeSpan? GetTypicalTime(IEnumerable<PackingHistoryEntry> history)
        {
            var durations = history
                .Where(h => h.ActiveDurationSeconds > 0)
                .OrderByDescending(h => h.CompletedDate)
                .Take(TypicalTimeSessionWindow)
                .Select(h => h.ActiveDurationSeconds!.Value)
                .Order()
                .ToList();

            if (durations.Count == 0)
                return null;

            var middle = durations.Count / 2;
            var median = durations.Count % 2 == 1
                ? durations[middle]
                : (durations[middle - 1] + durations[middle]) / 2.0;

            return TimeSpan.FromSeconds(median);
        }

        public async Task<List<PackingHistoryEntry>> GetRecentHistoryAsync(string activityId, int count = 10)
        {
            return await _historyRepository.GetHistoryForActivityAsync(activityId, count);
        }

        public async Task<PackingHistoryEntry?> GetLastPackingSessionAsync(string activityId)
        {
            var history = await _historyRepository.GetHistoryForActivityAsync(activityId, 1);
            return history.FirstOrDefault();
        }

        public double GetPackingEfficiency(int packedItems, int totalItems)
        {
            if (totalItems == 0)
                return 0;

            return Math.Round((double)packedItems / totalItems * 100, 1);
        }

        public async Task<PackingTimeComparison?> CompareWithTypicalAsync(string activityId, TimeSpan currentDuration)
        {
            var typical = await GetTypicalPackingTimeAsync(activityId);
            if (!typical.HasValue)
                return null;

            return new PackingTimeComparison
            {
                Typical = typical.Value,
                Difference = typical.Value - currentDuration
            };
        }
    }
}
