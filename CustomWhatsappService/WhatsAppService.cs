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
  

    public WhatsAppService(HttpClient httpClient, IOptions<WhatsSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
    }

    public async Task<WhatsAppMessageResponse> SendMessageAsync(WhatsAppMessageRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<WhatsAppMessageResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        return new WhatsAppMessageResponse
        {
            Success = false,
            Message = responseContent
        };
    }
}
