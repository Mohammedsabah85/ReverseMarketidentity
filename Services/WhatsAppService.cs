using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ReverseMarket.Services
{
    public class WhatsAppSettings
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly WhatsAppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(IOptions<WhatsAppSettings> settings, HttpClient httpClient, ILogger<WhatsAppService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            try
            {
                // للتطوير: حفظ الرسائل في ملف
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WhatsApp to {phoneNumber}: {message}";

                // طباعة في Console
                Console.WriteLine(logMessage);

                // كتابة في ملف
                var logPath = Path.Combine("wwwroot", "logs", "whatsapp.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                await File.AppendAllTextAsync(logPath, logMessage + Environment.NewLine);

                // تسجيل في Logger
                _logger.LogInformation("WhatsApp message sent to {PhoneNumber}: {Message}", phoneNumber, message);

                // محاكاة إرسال ناجح
                await Task.Delay(100); // محاكاة تأخير الشبكة
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendOTPAsync(string phoneNumber, string otp)
        {
            var message = $"🔐 رمز التحقق الخاص بك في السوق العكسي: {otp}\n\n" +
                         $"⚠️ لا تشارك هذا الرمز مع أي شخص.\n" +
                         $"⏰ الرمز صالح لمدة 10 دقائق فقط.\n\n" +
                         $"السوق العكسي 🛒";

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> SendLoginOTPAsync(string phoneNumber, string otp, string userName)
        {
            var message = $"🔑 مرحباً {userName}!\n\n" +
                         $"رمز تسجيل الدخول: {otp}\n\n" +
                         $"⚠️ لا تشارك هذا الرمز مع أحد.\n" +
                         $"⏰ صالح لمدة 10 دقائق.\n\n" +
                         $"السوق العكسي 🛒";

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> SendPhoneVerificationAsync(string phoneNumber, string verificationCode, bool isExistingUser = false)
        {
            string message;

            if (isExistingUser)
            {
                message = $"📱 تأكيد رقم الهاتف - السوق العكسي\n\n" +
                         $"رمز التأكيد: {verificationCode}\n\n" +
                         $"حسابك موجود ولكن يحتاج تأكيد رقم الهاتف.\n" +
                         $"بعد التأكيد ستتمكن من الدخول مباشرة.\n\n" +
                         $"⚠️ لا تشارك هذا الرمز مع أحد.\n" +
                         $"⏰ صالح لمدة 10 دقائق.\n\n" +
                         $"السوق العكسي 🛒";
            }
            else
            {
                message = $"🎉 مرحباً بك في السوق العكسي!\n\n" +
                         $"رمز تأكيد رقم الهاتف: {verificationCode}\n\n" +
                         $"بعد التأكيد ستحتاج لإكمال بيانات حسابك الجديد.\n\n" +
                         $"⚠️ لا تشارك هذا الرمز مع أحد.\n" +
                         $"⏰ صالح لمدة 10 دقائق.\n\n" +
                         $"السوق العكسي 🛒";
            }

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> SendWelcomeMessageAsync(string phoneNumber, string userName, string userType)
        {
            string message;

            if (userType == "Seller")
            {
                message = $"🎉 مرحباً {userName}!\n\n" +
                         $"تم إنشاء حساب متجرك بنجاح في السوق العكسي!\n\n" +
                         $"🏪 الآن يمكنك:\n" +
                         $"• تلقي طلبات من العملاء\n" +
                         $"• التواصل المباشر مع المشترين\n" +
                         $"• عرض منتجاتك وخدماتك\n\n" +
                         $"💡 نصيحة: تأكد من تحديث معلومات متجرك في الملف الشخصي\n\n" +
                         $"السوق العكسي 🛒";
            }
            else
            {
                message = $"🎉 مرحباً {userName}!\n\n" +
                         $"تم إنشاء حسابك بنجاح في السوق العكسي!\n\n" +
                         $"🛍️ الآن يمكنك:\n" +
                         $"• إضافة طلباتك\n" +
                         $"• تلقي عروض من المتاجر\n" +
                         $"• التواصل المباشر مع البائعين\n\n" +
                         $"💡 ابدأ بإضافة طلبك الأول!\n\n" +
                         $"السوق العكسي 🛒";
            }

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> NotifyStoreAsync(string phoneNumber, string storeName, string requestTitle, string requestUrl)
        {
            var message = $"🔔 طلب جديد - {storeName}!\n\n" +
                         $"📋 {requestTitle}\n\n" +
                         $"💰 فرصة جديدة لزيادة مبيعاتك!\n" +
                         $"🕒 تواصل مع العميل بسرعة للحصول على الطلب\n\n" +
                         $"👀 للمشاهدة والتواصل:\n" +
                         $"{requestUrl}\n\n" +
                         $"السوق العكسي 🛒";

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> NotifyUserApprovalAsync(string phoneNumber, string userName, string requestTitle)
        {
            var message = $"✅ تم اعتماد طلبك - {userName}!\n\n" +
                         $"📋 {requestTitle}\n\n" +
                         $"🎉 تم الموافقة على طلبك بنجاح!\n" +
                         $"📢 سيتم إشعار المتاجر المتخصصة\n" +
                         $"⏰ ستبدأ بتلقي العروض قريباً\n\n" +
                         $"💡 نصيحة: كن مستعداً للرد على المتاجر بسرعة\n\n" +
                         $"السوق العكسي 🛒";

            return await SendMessageAsync(phoneNumber, message);
        }

        public async Task<bool> NotifyRequestStatusAsync(string phoneNumber, string userName, string requestTitle, string status, string? adminNotes = null)
        {
            string message = $"📄 تحديث حالة طلبك - {userName}!\n\n";
            message += $"📋 {requestTitle}\n\n";

            switch (status.ToLower())
            {
                case "approved":
                    message += $"✅ تم اعتماد طلبك!\n";
                    message += $"📢 سيتم إشعار المتاجر المتخصصة\n";
                    message += $"⏰ ستبدأ بتلقي العروض قريباً\n";
                    break;

                case "rejected":
                    message += $"❌ تم رفض طلبك\n";
                    if (!string.IsNullOrEmpty(adminNotes))
                    {
                        message += $"📝 السبب: {adminNotes}\n";
                    }
                    message += $"💡 يمكنك إضافة طلب جديد بتعديل المتطلبات\n";
                    break;

                case "postponed":
                    message += $"⏸️ تم تأجيل طلبك مؤقتاً\n";
                    if (!string.IsNullOrEmpty(adminNotes))
                    {
                        message += $"📝 السبب: {adminNotes}\n";
                    }
                    message += $"⏰ سيتم مراجعته مرة أخرى قريباً\n";
                    break;
            }

            message += $"\nالسوق العكسي 🛒";

            return await SendMessageAsync(phoneNumber, message);
        }
    }
}