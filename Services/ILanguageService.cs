namespace ReverseMarket.Services
{
    //public interface ILanguageService
    //{
    //    string GetCurrentLanguage();
    //    string GetDirection();
    //    List<LanguageOption> GetSupportedLanguages();
    //    void SetLanguage(string language);
    //    bool IsLanguageSupported(string language);
    //}


    public interface ILanguageService
    {
        string GetCurrentLanguage();
        string GetDirection();
        List<LanguageOption> GetSupportedLanguages();
        void SetLanguage(string languageCode);
        bool IsLanguageSupported(string languageCode);
    }

    //public class LanguageOption
    //{
    //    public string Code { get; set; } = "";
    //    public string Name { get; set; } = "";
    //    public string NativeName { get; set; } = "";
    //    public string Direction { get; set; } = "ltr";
    //    public string FlagIcon { get; set; } = "";
    //}
}