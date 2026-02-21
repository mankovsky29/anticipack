using Anticipack.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Anticipack.Components.Features.Packing;

public partial class PackingDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IKeyboardService KeyboardService { get; set; } = default!;

    private ElementReference messageInput;
    private ElementReference messagesContainer;
    private DotNetObjectReference<PackingDialog>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    private string currentMessage = string.Empty;

    private List<ChatMessage> messages = new();

    // Keyboard
    private bool _keyboardVisible;
    private double _keyboardHeight;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            KeyboardService.Initialize(this);
            KeyboardService.KeyboardVisibilityChanged += OnKeyboardVisibilityChanged;

            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./Components/Features/Packing/PackingDialog.razor.js");

            // create a DotNet reference so JS can call back when Enter is pressed
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("attach", messageInput, _dotNetRef, messagesContainer);
            await messageInput.FocusAsync();
        }
    }

    private void OnKeyboardVisibilityChanged(bool isVisible, double height)
    {
        _keyboardVisible = isVisible;
        _keyboardHeight = height;

        if (isVisible)
        {
            _ = JS.InvokeVoidAsync("adjustPageForKeyboard", height);
            _ = JS.InvokeVoidAsync("scrollActiveElementIntoView");
        }
        else
        {
            _ = JS.InvokeVoidAsync("adjustPageForKeyboard", 0);
        }
    }

    private async Task Send()
    {
        var text = currentMessage?.TrimEnd();
        if (string.IsNullOrWhiteSpace(text))
            return;

        messages.Add(new ChatMessage
        {
            Text = text,
            Timestamp = DateTime.Now,
            IsSent = true
        });

        currentMessage = string.Empty;

        // allow UI to update then scroll to bottom and restore focus
        await InvokeAsync(async () =>
        {
            await Task.Yield();
            if (_jsModule is not null)
                await _jsModule.InvokeVoidAsync("scrollToBottom", messagesContainer);
            await messageInput.FocusAsync();
        });
    }

    [JSInvokable]
    public Task SubmitFromJs() => Send();

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

    private sealed class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSent { get; set; }
    }
}
