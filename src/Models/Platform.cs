using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class Platform
    {
        [JsonProperty("name", Required = Required.Always)]
        [OpenApiProperty(Description = "Name of the platform")]
        public string? Name { get; set; }

        [JsonProperty("modules", Required = Required.Always)]
        [OpenApiProperty(Description = "Dictionary of the service's core modules")]
        public Dictionary<ModuleType, Module>? Modules { get; set; }
    }
}
