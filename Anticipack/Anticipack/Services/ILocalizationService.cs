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
            get
            {
                // Always check if the culture matches the saved preference
                var savedLanguage = Preferences.Default.Get("AppLanguage", string.Empty);
                if (!string.IsNullOrEmpty(savedLanguage) && _currentCulture?.Name != savedLanguage)
                {
                    _currentCulture = new CultureInfo(savedLanguage);
                    ApplyCulture(_currentCulture);
                }
                return _currentCulture;
            }
            private set
            {
                if (_currentCulture?.Name != value?.Name)
                {
                    _currentCulture = value;
                    ApplyCulture(value);
                    
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
            
            ApplyCulture(_currentCulture);
        }

        public void SetCulture(string culture)
        {
            CurrentCulture = new CultureInfo(culture);
        }

        public string GetLocalizedCategory(string categoryKey)
        {
            return $"Category_{categoryKey}";
        }

        private void ApplyCulture(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}