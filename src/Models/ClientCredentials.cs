using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class ClientCredentials
    {
        [JsonProperty("client_id", Required = Required.Always)]
        [OpenApiProperty(Description = "Client identifier")]
        public string? ClientId { get; set; }

        [JsonProperty("client_secret", Required = Required.AllowNull)]
        [OpenApiProperty(Description = "Client secret")]
        public string? ClientSecret { get; set; }
    }
}
