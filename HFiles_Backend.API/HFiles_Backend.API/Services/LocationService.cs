using HFiles_Backend.Application.DTOs.Labs;
using Newtonsoft.Json;

namespace HFiles_Backend.API.Services
{
    public class LocationService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<string> GetLocationDetails(string? pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode))
                return "Invalid pincode";

            var response = await _httpClient.GetAsync($"https://api.postalpincode.in/pincode/{pincode}");

            if (!response.IsSuccessStatusCode)
                return $"Failed to fetch location details (Status Code: {response.StatusCode})";

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var postalData = JsonConvert.DeserializeObject<List<LocationDetailsResponse>>(jsonResponse);

            if (postalData == null || postalData.Count == 0 || postalData[0].Status != "Success")
                return $"Location not found for pincode {pincode}";

            var locationDetails = postalData[0].PostOffice?.FirstOrDefault();

            return locationDetails != null
                ? $"{locationDetails.Name}, {locationDetails.District}, {locationDetails.State}"
                : "Location not found";
        }
    }

}
