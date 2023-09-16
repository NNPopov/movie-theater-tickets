using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

//using Newtonsoft.Json;

namespace CinemaTicketBooking.Application.LoadTests
{
    public static class JsonSerializerExtensions
    {
        public const string JsonFormatType = "application/json";

        public static HttpContent ToHttpContent<T>(this T request) where T : class
        {
            
            var opts = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };


            string jsonRequest = JsonSerializer.Serialize(request, opts);
            return new StringContent(jsonRequest, Encoding.UTF8, JsonFormatType);
        }

        public static async Task<T> DeserializeHttpResponseAsync<T>(this HttpResponseMessage response)
        {
            var opts = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNameCaseInsensitive = true
            };
            var responseContent = await response.Content.ReadAsStringAsync();
            return  JsonSerializer.Deserialize<T>(responseContent,opts);
        }

    }
}
