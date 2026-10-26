namespace Anticipack.Services.Statistics;

/// <summary>
/// Computes the personal packing summary and list health insights
/// </summary>
public interface IPackingStatisticsService
{
    Task<PackingInsights> GetInsightsAsync();
}
