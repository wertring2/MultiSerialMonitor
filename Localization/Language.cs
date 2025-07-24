namespace MultiSerialMonitor.Localization
{
    public enum Language
    {
        English,
        Thai
    }
    
    public class LanguageInfo
    {
        public Language Language { get; set; }
        public string DisplayName { get; set; } = "";
        public string CultureCode { get; set; } = "";
        
        public static LanguageInfo[] AvailableLanguages = new[]
        {
            new LanguageInfo { Language = Language.English, DisplayName = "English", CultureCode = "en-US" },
            new LanguageInfo { Language = Language.Thai, DisplayName = "ไทย", CultureCode = "th-TH" }
        };
    }
}