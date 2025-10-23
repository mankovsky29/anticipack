using Anticipack.Services;

namespace Anticipack.Platforms.Windows
{
    internal class KeyboardService : IKeyboardService
    {
        public event Action<bool, double>? KeyboardVisibilityChanged;

        public void Initialize(object? platformSpecific)
        {
            // do nothing
        }
    }
}
