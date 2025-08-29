namespace ReverseMarket.Services
{
    public interface IWhatsAppService
    {
        /// <summary>
        /// إرسال رسالة واتساب عامة
        /// </summary>
        Task<bool> SendMessageAsync(string phoneNumber, string message);

        /// <summary>
        /// إرسال رمز OTP عام (للتحقق أو الدخول)
        /// </summary>
        Task<bool> SendOTPAsync(string phoneNumber, string otp);

        /// <summary>
        /// إرسال رمز تسجيل الدخول للمستخدمين المؤكدين
        /// </summary>
        Task<bool> SendLoginOTPAsync(string phoneNumber, string otp, string userName);

        /// <summary>
        /// إرسال رمز تأكيد الهاتف للمستخدمين الجدد أو غير المؤكدين
        /// </summary>
        Task<bool> SendPhoneVerificationAsync(string phoneNumber, string verificationCode, bool isExistingUser = false);

        /// <summary>
        /// إرسال رسالة ترحيب للمستخدمين الجدد
        /// </summary>
        Task<bool> SendWelcomeMessageAsync(string phoneNumber, string userName, string userType);

        /// <summary>
        /// إشعار المتاجر بطلب جديد
        /// </summary>
        Task<bool> NotifyStoreAsync(string phoneNumber, string storeName, string requestTitle, string requestUrl);

        /// <summary>
        /// إشعار المستخدم بالموافقة على طلبه
        /// </summary>
        Task<bool> NotifyUserApprovalAsync(string phoneNumber, string userName, string requestTitle);

        /// <summary>
        /// إشعار المستخدم بتحديث حالة طلبه
        /// </summary>
        Task<bool> NotifyRequestStatusAsync(string phoneNumber, string userName, string requestTitle, string status, string? adminNotes = null);
    }
}