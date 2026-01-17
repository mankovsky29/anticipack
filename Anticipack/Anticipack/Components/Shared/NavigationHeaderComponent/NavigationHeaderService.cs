namespace Anticipack.Components.Shared.NavigationHeaderComponent
{
    public class NavigationHeaderService : INavigationHeaderService
    {
        // Events that the NavigationHeader component will subscribe to
        public event Action<string>? OnTextChanged;
        public event Action<bool>? OnNavMenuToggled;

        private string _currentText = string.Empty;
        private bool _isNavMenuExpanded = false;

        public void SetText(string text)
        {
            _currentText = text ?? string.Empty;
            OnTextChanged?.Invoke(_currentText);
        }

        public string GetText()
        {
            return _currentText;
        }

        public void SetNavMenuExpanded(bool isExpanded)
        {
            _isNavMenuExpanded = isExpanded;
            OnNavMenuToggled?.Invoke(_isNavMenuExpanded);
        }

        public bool IsNavMenuExpanded()
        {
            return _isNavMenuExpanded;
        }
    }
}
