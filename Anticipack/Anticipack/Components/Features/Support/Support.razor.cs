using Anticipack.Components.Shared.NavigationHeaderComponent;
using Anticipack.Resources.Localization;
using Anticipack.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Anticipack.Components.Features.Support;

public partial class Support : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocalizer<AppResources> L { get; set; } = default!;
    [Inject] private INavigationHeaderService NavigationHeaderService { get; set; } = default!;
    [Inject] private IKeyboardService KeyboardService { get; set; } = default!;

    private ElementReference messageInput;
    private ElementReference messagesContainer;
    private DotNetObjectReference<Support>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    private string currentMessage = string.Empty;
    private List<SupportMessage> messages = new();
    private bool CanSend => !string.IsNullOrWhiteSpace(currentMessage);

    // Keyboard
    private bool _keyboardVisible;
    private double _keyboardHeight;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            NavigationHeaderService.SetText(L["Support"]);

            KeyboardService.Initialize(this);
            KeyboardService.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;

            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Features/Support/Support.razor.js");

            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("attach", messageInput, _dotNetRef, messagesContainer);

            // Show welcome message
            await Task.Delay(500);
            messages.Add(new SupportMessage
            {
                Text = L["WelcomeToSupport"],
                Timestamp = DateTime.Now,
                IsSent = false,
                Status = MessageStatus.None
            });
            StateHasChanged();
            await ScrollToBottom();
        }
    }

    private void OnKeyboardVisibilityChanged(bool isVisible, double height)
    {
        _keyboardVisible = isVisible;
        _keyboardHeight = height;

        if (isVisible)
        {
            _ = JS.InvokeVoidAsync("adjustPageForKeyboard", height);
            _ = JS.InvokeVoidAsync("scrollActiveElementIntoView", height);
        }
        else
        {
            _ = JS.InvokeVoidAsync("adjustPageForKeyboard", 0);
        }
    }

    private async Task SendMessage()
    {
        var text = currentMessage?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var message = new SupportMessage
        {
            Text = text,
            Timestamp = DateTime.Now,
            IsSent = true,
            Status = MessageStatus.Sending
        };

        messages.Add(message);
        currentMessage = string.Empty;
        StateHasChanged();
        await ScrollToBottom();

        // Simulate sending to support
        await Task.Delay(1500);
        message.Status = MessageStatus.Sent;
        StateHasChanged();

        // Simulate auto-response after a delay
        await Task.Delay(2000);
        messages.Add(new SupportMessage
        {
            Text = L["AutoReplyMessage"],
            Timestamp = DateTime.Now,
            IsSent = false,
            Status = MessageStatus.None
        });
        StateHasChanged();
        await ScrollToBottom();
    }

    [JSInvokable]
    public Task SubmitFromJs() => SendMessage();

    private async Task ScrollToBottom()
    {
        try
        {
            if (_jsModule is not null)
                await _jsModule.InvokeVoidAsync("scrollToBottom", messagesContainer);
        }
        catch
        {
            // Ignore if JS not ready
        }
    }

    public async ValueTask DisposeAsync()
    {
        KeyboardService.KeyboardVisibilityChanged -= OnKeyboardVisibilityChanged;

        if (_dotNetRef is not null)
        {
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }

        try
        {
            if (_jsModule is not null)
            {
                await _jsModule.InvokeVoidAsync("detach", messageInput);
                await _jsModule.DisposeAsync();
            }
        }
        catch
        {
            // ignore if JS side not present
        }
    }

    private enum MessageStatus
    {
        None,
        Sending,
        Sent,
        Failed
    }

    private sealed class SupportMessage
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSent { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.None;
    }
}
