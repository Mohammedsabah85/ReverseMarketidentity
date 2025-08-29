using Microsoft.Extensions.Localization;

namespace ReverseMarket.Services
{
    public interface ILanguageService
    {
        /// <summary>
        /// الحصول على رمز اللغة الحالية
        /// </summary>
        /// <returns>رمز اللغة (ar, en, ku)</returns>
        string GetCurrentLanguage();

        /// <summary>
        /// الحصول على اتجاه النص للغة الحالية
        /// </summary>
        /// <returns>rtl أو ltr</returns>
        string GetDirection();

        /// <summary>
        /// الحصول على قائمة اللغات المدعومة
        /// </summary>
        /// <returns>قائمة اللغات المدعومة</returns>
        List<LanguageOption> GetSupportedLanguages();

        /// <summary>
        /// تحديد اللغة الحالية
        /// </summary>
        /// <param name="languageCode">رمز اللغة</param>
        void SetLanguage(string languageCode);

        /// <summary>
        /// التحقق من دعم اللغة
        /// </summary>
        /// <param name="languageCode">رمز اللغة</param>
        /// <returns>true إذا كانت اللغة مدعومة</returns>
        bool IsLanguageSupported(string languageCode);

        /// <summary>
        /// الحصول على معلومات اللغة
        /// </summary>
        /// <param name="languageCode">رمز اللغة</param>
        /// <returns>معلومات اللغة أو null</returns>
        LanguageOption? GetLanguageInfo(string languageCode);
    }

    public class LanguageOption
    {
        /// <summary>
        /// رمز اللغة (ar, en, ku)
        /// </summary>
        public string Code { get; set; } = "";

        /// <summary>
        /// اسم اللغة بالإنجليزية
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// اسم اللغة بلغتها الأصلية
        /// </summary>
        public string NativeName { get; set; } = "";

        /// <summary>
        /// علم البلد
        /// </summary>
        public string Flag { get; set; } = "";

        /// <summary>
        /// اتجاه النص (rtl أو ltr)
        /// </summary>
        public string Direction { get; set; } = "ltr";
    }
}