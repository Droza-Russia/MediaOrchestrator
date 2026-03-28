using System;
using System.Globalization;

namespace MediaOrchestrator
{
    internal static class LocalizationManager
    {
        private static readonly object _sync = new object();
        private static LocalizationLanguage _currentLanguage = LocalizationLanguage.English;

        static LocalizationManager()
        {
            Initialize(LocalizationLanguage.English);
        }

        internal static LocalizationLanguage CurrentLanguage
        {
            get
            {
                lock (_sync)
                {
                    return _currentLanguage;
                }
            }
        }

        internal static void Initialize(LocalizationLanguage language = LocalizationLanguage.English)
        {
            lock (_sync)
            {
                _currentLanguage = language;
            }
        }

        internal static void InitializeFromCulture()
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            lock (_sync)
            {
                switch (culture)
                {
                    case "en":
                        _currentLanguage = LocalizationLanguage.English;
                        break;
                    case "de":
                        _currentLanguage = LocalizationLanguage.German;
                        break;
                    default:
                        _currentLanguage = LocalizationLanguage.English;
                        break;
                }
            }
        }
    }
}
