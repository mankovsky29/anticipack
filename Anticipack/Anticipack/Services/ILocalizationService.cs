using System.Globalization;

namespace Anticipack.Services
{
    public interface ILocalizationService
    {
        CultureInfo CurrentCulture { get; }
        event EventHandler<CultureInfo>? CultureChanged;
        void SetCulture(string culture);
        string GetLocalizedCategory(string categoryKey);
    }

    public class LocalizationService : ILocalizationService
    {
        private CultureInfo _currentCulture;
        
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture?.Name != value?.Name)
                {
                    _currentCulture = value;
                    CultureInfo.CurrentCulture = value;
                    CultureInfo.CurrentUICulture = value;
                    
                    // Persist the preference
                    Preferences.Default.Set("AppLanguage", value.Name);
                    
                    CultureChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<CultureInfo>? CultureChanged;

        public LocalizationService()
        {
            // Load saved language preference or use system language
            var savedLanguage = Preferences.Default.Get("AppLanguage", string.Empty);
            
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                _currentCulture = new CultureInfo(savedLanguage);
            }
            else
            {
                _currentCulture = CultureInfo.CurrentCulture;
            }
            
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentCulture;
        }

        public void SetCulture(string culture)
        {
            CurrentCulture = new CultureInfo(culture);
        }

        public string GetLocalizedCategory(string categoryKey)
        {
            return $"Category_{categoryKey}";
        }
    }
}