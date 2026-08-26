using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace University_Agent_System.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAgentCredentialsWhatsAppAsync(
            string toPhone,
            string agentName,
            int? agentCode,
            string password,
            string loginUrl)
        {
            string accessToken = _configuration["WhatsApp:AccessToken"];
            string phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            string apiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v23.0";
            string templateName = _configuration["WhatsApp:AgentCredentialsTemplateName"];

            if (string.IsNullOrWhiteSpace(toPhone))
                throw new Exception("WhatsApp phone number is empty.");

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("WhatsApp AccessToken is missing.");

            if (string.IsNullOrWhiteSpace(phoneNumberId))
                throw new Exception("WhatsApp PhoneNumberId is missing.");

            if (string.IsNullOrWhiteSpace(templateName))
                throw new Exception("WhatsApp template name is missing.");

            string url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhone.Replace(" ", ""),
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new
                    {
                        code = "en"
                    },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = agentName ?? "" },
                                new { type = "text", text = agentCode.ToString() ?? "" },
                                new { type = "text", text = password ?? "" },
                                new { type = "text", text = loginUrl ?? "" }
                            }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WhatsApp send failed. Status: {Status}, Body: {Body}", response.StatusCode, responseBody);
                throw new Exception($"WhatsApp API error: {response.StatusCode} - {responseBody}");
            }

            _logger.LogInformation("WhatsApp sent to {Phone}. Response: {Response}", toPhone, responseBody);
        }

        public async Task<string> SendTestMessageAsync(string toPhone)
        {
            string accessToken = _configuration["WhatsApp:AccessToken"];
            string phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            string apiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v23.0";
            string templateName = _configuration["WhatsApp:AgentCredentialsTemplateName"] ?? "hello_world";

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("WhatsApp AccessToken is missing.");

            if (string.IsNullOrWhiteSpace(phoneNumberId))
                throw new Exception("WhatsApp PhoneNumberId is missing.");

            if (string.IsNullOrWhiteSpace(toPhone))
                throw new Exception("Recipient phone is empty.");

            // WhatsApp API expects digits only in "to"
            string cleanedPhone = toPhone.Trim()
                                         .Replace(" ", "")
                                         .Replace("-", "")
                                         .TrimStart('+');

            string url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = cleanedPhone,
                type = "template",
                template = new
                {
                    name = templateName,   // for test use hello_world
                    language = new
                    {
                        code = "en_US"     // important for hello_world
                    }
                }
            };

            string json = System.Text.Json.JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"WhatsApp API error: {response.StatusCode} - {responseBody}");

            return responseBody;
        }

    }

}