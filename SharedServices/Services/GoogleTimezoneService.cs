using CommonLibrary.Models;
using CommonLibrary.Models.API;
using CommonLibrary.SharedServices.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;

namespace CommonLibrary.SharedServices.Services
{
    public class GoogleTimezoneService : IGoogleTimezoneService
    {
        public async Task<ServiceResponse> GetGoogleApiResponse(GoogleTimezoneApiPayload payload)
        {
            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_TIMEZONE_API_KEY");
            var builder = new UriBuilder("https://maps.googleapis.com/maps/api/timezone/json");

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["location"] = payload.location;
            query["timestamp"] = payload.timestamp;
            query["key"] = apiKey;

            builder.Query = query.ToString();

            var finalUrl = builder.ToString();

            using var client = new HttpClient();
            var response = await client.GetAsync(finalUrl);
            //response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var respnseJson = JsonConvert.DeserializeObject<JObject>(json);

            return new ServiceResponse { Result = respnseJson, StatusCode = ((int)response.StatusCode)};
        }
    }
}
