using Anticipack.Resources.Localization;
using Anticipack.Services.Packing;
using Microsoft.Extensions.Localization;

namespace Anticipack.Components.Features.Packing;

/// <summary>
/// Formats packing durations as short, localized text such as "12 min" or "1 h 5 min".
/// </summary>
public static class PackingDurationFormatter
{
    public static string Format(TimeSpan duration, IStringLocalizer<AppResources> localizer)
    {
        if (duration.TotalMinutes < 1)
            return localizer["DurationUnderMinute"];

        var totalMinutes = (int)Math.Round(duration.TotalMinutes);
        if (totalMinutes < 60)
            return string.Format(localizer["DurationMinutesShort"], totalMinutes);

        return string.Format(localizer["DurationHoursMinutesShort"], totalMinutes / 60, totalMinutes % 60);
    }

    public static string FormatComparison(PackingTimeComparison comparison, IStringLocalizer<AppResources> localizer)
    {
        // Differences under a minute are noise, not something worth celebrating or worrying about
        var difference = comparison.Difference.Duration();
        if (difference < TimeSpan.FromMinutes(1))
            return localizer["AboutAsUsual"];

        var key = comparison.IsFaster ? "FasterThanUsual" : "SlowerThanUsual";
        return string.Format(localizer[key], Format(difference, localizer));
    }
}
