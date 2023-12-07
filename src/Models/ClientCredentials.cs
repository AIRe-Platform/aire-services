using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class ClientCredentials
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
    }
}
