using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Resources.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Anticipack.Components.Features.Support;

public partial class Support
{
    private const string SupportEmail = "support@anticipack.com";

    [Inject] private IStringLocalizer<AppResources> L { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationHeaderService.SetText(L["Support"]);
    }
}
