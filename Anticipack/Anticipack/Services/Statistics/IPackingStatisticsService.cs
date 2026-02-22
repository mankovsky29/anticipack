namespace Anticipack.Services.Statistics;

/// <summary>
/// Service for computing aggregated packing statistics
/// </summary>
public interface IPackingStatisticsService
{
    Task<PackingStatisticsData> GetStatisticsAsync();
}
