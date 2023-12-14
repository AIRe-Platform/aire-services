using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class PlatformConfiguration
    {
        [JsonProperty("platform", Required = Required.Always)]
        [OpenApiProperty(Description = "Platform details")]
        public Platform? Platform { get; set; }

        [JsonProperty("services", Required = Required.Always)]
        [OpenApiProperty(Description = "Available third-party services on the platform")]
        public List<Service>? Services { get; set; }
    }
}