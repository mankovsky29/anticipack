using Anticipack.Storage;

namespace Anticipack.Services.Packing
{
    public class PackingHistoryService : IPackingHistoryService
    {
        private readonly IPackingRepository _repository;

        public PackingHistoryService(IPackingRepository repository)
        {
            _repository = repository;
        }

        public async Task RecordPackingSessionAsync(string activityId, DateTime startTime, DateTime endTime, int totalItems, int packedItems)
        {
            var entry = new PackingHistoryEntry
            {
                ActivityId = activityId,
                StartTime = startTime,
                EndTime = endTime,
                CompletedDate = endTime,
                TotalItems = totalItems,
                PackedItems = packedItems,
                DurationSeconds = (int)(endTime - startTime).TotalSeconds
            };

            await _repository.AddHistoryEntryAsync(entry);
        }

        public async Task<TimeSpan?> GetAveragePackingTimeAsync(string activityId)
        {
            var history = await _repository.GetHistoryForActivityAsync(activityId);
            
            if (!history.Any())
                return null;

            var averageSeconds = history.Average(h => h.DurationSeconds);
            return TimeSpan.FromSeconds(averageSeconds);
        }

        public async Task<List<PackingHistoryEntry>> GetRecentHistoryAsync(string activityId, int count = 10)
        {
            return await _repository.GetHistoryForActivityAsync(activityId, count);
        }

        public async Task<PackingHistoryEntry?> GetLastPackingSessionAsync(string activityId)
        {
            var history = await _repository.GetHistoryForActivityAsync(activityId, 1);
            return history.FirstOrDefault();
        }

        public double GetPackingEfficiency(int packedItems, int totalItems)
        {
            if (totalItems == 0)
                return 0;

            return Math.Round((double)packedItems / totalItems * 100, 1);
        }

        public async Task<PackingTimeComparison> CompareWithAverageAsync(string activityId, TimeSpan currentDuration)
        {
            var averageTime = await GetAveragePackingTimeAsync(activityId);
            
            if (!averageTime.HasValue)
            {
                return new PackingTimeComparison
                {
                    IsFaster = false,
                    Difference = TimeSpan.Zero,
                    FormattedDifference = "No history available"
                };
            }

            var difference = averageTime.Value - currentDuration;
            var isFaster = difference > TimeSpan.Zero;
            var absDifference = difference.Duration();

            string formatted;
            if (absDifference.TotalMinutes < 1)
            {
                formatted = isFaster 
                    ? $"{(int)absDifference.TotalSeconds} seconds faster"
                    : $"{(int)absDifference.TotalSeconds} seconds slower";
            }
            else
            {
                formatted = isFaster 
                    ? $"{(int)absDifference.TotalMinutes} minutes faster"
                    : $"{(int)absDifference.TotalMinutes} minutes slower";
            }

            return new PackingTimeComparison
            {
                IsFaster = isFaster,
                Difference = difference,
                FormattedDifference = formatted
            };
        }

        public async Task<TimeSpan> EstimateRemainingTimeAsync(string activityId, int itemsRemaining)
        {
            var history = await _repository.GetHistoryForActivityAsync(activityId);
            
            if (!history.Any())
            {
                // Default estimate: 30 seconds per item
                return TimeSpan.FromSeconds(itemsRemaining * 30);
            }

            // Calculate average time per item from history
            var averageTimePerItem = history
                .Where(h => h.TotalItems > 0)
                .Average(h => (double)h.DurationSeconds / h.TotalItems);

            return TimeSpan.FromSeconds(itemsRemaining * averageTimePerItem);
        }
    }
}
