namespace Anticipack.Services
{
    public interface IKeyboardService
    {
        event Action<bool>? KeyboardVisibilityChanged;

        // optional: used by platform init code
        void Initialize(object? platformSpecific);
    }
}
