using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace HFiles_Backend.API.Services
{
    public class WhatsappService : IWhatsappService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _apiKey;

        public WhatsappService(IConfiguration config, HttpClient httpClient)
        {
            _apiUrl = config["Interakt:ApiUrl"] ?? throw new ArgumentNullException(nameof(config), "Interakt:ApiUrl configuration is missing.");
            _apiKey = config["Interakt:ApiKey"] ?? throw new ArgumentNullException(nameof(config), "Interakt:ApiKey configuration is missing.");

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task SendOtpAsync(string otp, string mobileNo)
        {
            string countryCodeDigit = "";
            string pureMobileNumber = "";

            Match match = Regex.Match(mobileNo, @"^(\+?\d{1,4})?(\d{10})$");

            if (match.Success)
            {
                countryCodeDigit = match.Groups[1].Value;
                pureMobileNumber = match.Groups[2].Value;

                if (string.IsNullOrEmpty(countryCodeDigit))
                    countryCodeDigit = "+91";
            }
            else
            {
                throw new ArgumentException("Invalid mobile number format.");
            }

            var requestBody = new
            {
                countryCode = countryCodeDigit,
                phoneNumber = pureMobileNumber,
                type = "Template",
                callbackData = "otp_callback",
                template = new
                {
                    name = "otp_template",
                    languageCode = "en",
                    headerValues = new[] { "OTP" },
                    bodyValues = new[] { otp },
                    buttonValues = new
                    {
                        _0 = new[] { otp }
                    }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody).Replace("_0", "0");
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_apiKey}");

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send WhatsApp message. {_apiUrl} responded with {response.StatusCode}: {error}");
            }
        }
    }
}
