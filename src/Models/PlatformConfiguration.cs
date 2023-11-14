using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class PlatformConfiguration
    {
        [JsonProperty("platform")]
        [OpenApiProperty(Description = "Platform details")]
        public Platform Platform { get; set; }

        [JsonProperty("services")]
        [OpenApiProperty(Description = "Available third-party services on the platform")]
        public List<Service> Services { get; set; }
    }
}