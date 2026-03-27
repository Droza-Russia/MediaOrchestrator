using System;
using System.Globalization;

namespace Xabe.FFmpeg
{
    internal static class LocalizationManager
    {
        internal static LocalizationLanguage CurrentLanguage { get; private set; } = LocalizationLanguage.Russian;

        internal static void Initialize(LocalizationLanguage language = LocalizationLanguage.Russian)
        {
            CurrentLanguage = language;
        }

        internal static void InitializeFromCulture()
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            switch (culture)
            {
                case "en":
                    CurrentLanguage = LocalizationLanguage.English;
                    break;
                case "de":
                    CurrentLanguage = LocalizationLanguage.German;
                    break;
                default:
                    CurrentLanguage = LocalizationLanguage.Russian;
                    break;
            }
        }
    }
}
