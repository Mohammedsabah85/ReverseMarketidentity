using Microsoft.Extensions.Options;
using ReverseMarket.CustomWhatsappService;
using ReverseMarket.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IOptions<WhatsSettings> options, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
    }

    public async Task<WhatsAppMessageResponse> SendMessageAsync(WhatsAppMessageRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("📤 إرسال طلب WhatsApp إلى: {Url}", _settings.ApiUrl);
            _logger.LogInformation("📤 البيانات المرسلة: {Json}", json);

            var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("📥 حالة الرد: {StatusCode}", response.StatusCode);
            _logger.LogInformation("📥 محتوى الرد: {Response}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                // ✅ التعامل مع الحالات المختلفة للرد

                // إذا كان الرد رقمًا فقط (message ID)
                if (long.TryParse(responseContent.Trim(), out long messageId))
                {
                    _logger.LogInformation("✅ تم إرسال الرسالة بنجاح - معرف الرسالة: {MessageId}", messageId);
                    return new WhatsAppMessageResponse
                    {
                        Success = true,
                        Message = $"تم الإرسال بنجاح - معرف الرسالة: {messageId}",
                        Data = messageId
                    };
                }

                // إذا كان الرد JSON
                try
                {
                    var jsonResponse = JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (jsonResponse != null)
                    {
                        _logger.LogInformation("✅ تم إرسال الرسالة بنجاح - JSON Response");
                        return jsonResponse;
                    }
                }
                catch (JsonException)
                {
                    // ليس JSON صالح، لكن الطلب نجح
                }

                // الرد ناجح لكن التنسيق غير متوقع
                _logger.LogInformation("✅ تم إرسال الرسالة بنجاح");
                return new WhatsAppMessageResponse
                {
                    Success = true,
                    Message = "تم الإرسال بنجاح",
                    Data = responseContent
                };
            }

            // فشل الطلب
            _logger.LogError("❌ فشل إرسال الرسالة: {StatusCode} - {Response}",
                response.StatusCode, responseContent);

            return new WhatsAppMessageResponse
            {
                Success = false,
                Message = $"فشل الإرسال: {response.StatusCode} - {responseContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ خطأ في إرسال رسالة WhatsApp");
            return new WhatsAppMessageResponse
            {
                Success = false,
                Message = $"خطأ: {ex.Message}"
            };
        }
    }
}