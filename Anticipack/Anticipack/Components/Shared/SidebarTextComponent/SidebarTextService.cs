namespace Anticipack.Components.Shared.SidebarTextComponent
{
    public class SidebarTextService : ISidebarTextService
    {
        // Events that the SidebarText component will subscribe to
        public event Action<string>? OnTextChanged;
        public event Action<string?>? OnPackingIdChanged;
        public event Action<bool>? OnNavMenuToggled;

        private string _currentText = string.Empty;
        private string? _currentPackingId;
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

        public void SetPackingId(string? packingId)
        {
            _currentPackingId = packingId;
            OnPackingIdChanged?.Invoke(_currentPackingId);
        }

        public string? GetPackingId()
        {
            return _currentPackingId;
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
