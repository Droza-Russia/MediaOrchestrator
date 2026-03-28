using System.Globalization;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class LocalizationTests
    {
        [Fact]
        public void InitializeFromCulture_UsesEnglishForEnglishCulture()
        {
            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("en-US");

                LocalizationManager.InitializeFromCulture();

                Assert.Equal(LocalizationLanguage.English, LocalizationManager.CurrentLanguage);
                Assert.Equal("Conversion has already been started.", ErrorMessages.ConversionAlreadyStarted);
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalCulture;
                LocalizationManager.Initialize(LocalizationLanguage.Russian);
            }
        }

        [Fact]
        public void SetExecutablesPath_UsesExplicitLanguage()
        {
            FFmpeg.SetExecutablesPath(null, language: LocalizationLanguage.German);

            Assert.Equal(LocalizationLanguage.German, LocalizationManager.CurrentLanguage);
            Assert.Equal("Die Konvertierung wurde bereits gestartet.", ErrorMessages.ConversionAlreadyStarted);
        }

        [Fact]
        public void SetExecutablesPath_DefaultLanguage_IsRussian()
        {
            FFmpeg.SetExecutablesPath(null);

            Assert.Equal(LocalizationLanguage.Russian, LocalizationManager.CurrentLanguage);
            Assert.Equal("Конвертация уже была запущена.", ErrorMessages.ConversionAlreadyStarted);
        }

        [Fact]
        public void SetLocalizationLanguage_OverridesCurrentLanguage()
        {
            FFmpeg.SetExecutablesPath(null, language: LocalizationLanguage.Russian);
            FFmpeg.SetLocalizationLanguage(LocalizationLanguage.English);

            Assert.Equal(LocalizationLanguage.English, LocalizationManager.CurrentLanguage);
            Assert.Equal("Conversion has already been started.", ErrorMessages.ConversionAlreadyStarted);
        }
    }
}
