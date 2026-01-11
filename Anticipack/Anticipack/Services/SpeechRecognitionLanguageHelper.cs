using System.Globalization;

namespace Anticipack.Services
{
    /// <summary>
    /// Helper class to map app language codes to speech recognition language codes
    /// </summary>
    public static class SpeechRecognitionLanguageHelper
    {
        /// <summary>
        /// Maps a culture code to the corresponding speech recognition language code
        /// </summary>
        /// <param name="cultureCode">The culture code (e.g., "en", "es", "ru")</param>
        /// <returns>The speech recognition language code (e.g., "en-US", "es-ES", "ru-RU")</returns>
        public static string GetSpeechRecognitionLanguage(string cultureCode)
        {
            return cultureCode?.ToLowerInvariant() switch
            {
                "en" => "en-US",
                "es" => "es-ES",
                "ru" => "ru-RU",
                "fr" => "fr-FR",
                "de" => "de-DE",
                "it" => "it-IT",
                "pt" => "pt-PT",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
                "zh" or "zh-cn" => "zh-CN",
                "zh-tw" => "zh-TW",
                "ar" => "ar-SA",
                "hi" => "hi-IN",
                "nl" => "nl-NL",
                "pl" => "pl-PL",
                "tr" => "tr-TR",
                "sv" => "sv-SE",
                "da" => "da-DK",
                "fi" => "fi-FI",
                "no" => "nb-NO",
                "cs" => "cs-CZ",
                "el" => "el-GR",
                "he" => "he-IL",
                "th" => "th-TH",
                "vi" => "vi-VN",
                "id" => "id-ID",
                "uk" => "uk-UA",
                _ => "en-US" // Default fallback
            };
        }

        /// <summary>
        /// Gets the speech recognition language for the current culture
        /// </summary>
        /// <param name="culture">The current culture</param>
        /// <returns>The speech recognition language code</returns>
        public static string GetSpeechRecognitionLanguage(CultureInfo culture)
        {
            if (culture == null)
                return "en-US";

            // Try with full culture name first (e.g., "en-US")
            var fullName = culture.Name;
            if (!string.IsNullOrEmpty(fullName) && fullName.Contains('-'))
                return fullName;

            // Fallback to two-letter ISO language name
            return GetSpeechRecognitionLanguage(culture.TwoLetterISOLanguageName);
        }

        /// <summary>
        /// Gets available speech recognition languages with their display names
        /// </summary>
        /// <returns>Dictionary of language code to display name</returns>
        public static Dictionary<string, string> GetAvailableLanguages()
        {
            return new Dictionary<string, string>
            {
                { "en-US", "English (US)" },
                { "en-GB", "English (UK)" },
                { "es-ES", "Espa?ol (Espa?a)" },
                { "es-MX", "Espa?ol (M?xico)" },
                { "ru-RU", "Русский" },
                { "fr-FR", "Fran?ais" },
                { "de-DE", "Deutsch" },
                { "it-IT", "Italiano" },
                { "pt-PT", "Portugu?s (Portugal)" },
                { "pt-BR", "Portugu?s (Brasil)" },
                { "ja-JP", "???" },
                { "ko-KR", "???" },
                { "zh-CN", "?? (??)" },
                { "zh-TW", "?? (??)" },
                { "ar-SA", "???????" },
                { "hi-IN", "??????" },
                { "nl-NL", "Nederlands" },
                { "pl-PL", "Polski" },
                { "tr-TR", "T?rk?e" },
                { "sv-SE", "Svenska" },
                { "da-DK", "Dansk" },
                { "fi-FI", "Suomi" },
                { "nb-NO", "Norsk" },
                { "cs-CZ", "?e?tina" },
                { "el-GR", "????????" },
                { "he-IL", "?????" },
                { "th-TH", "???" },
                { "vi-VN", "Ti?ng Vi?t" },
                { "id-ID", "Bahasa Indonesia" },
                { "uk-UA", "Українська" }
            };
        }
    }
}
