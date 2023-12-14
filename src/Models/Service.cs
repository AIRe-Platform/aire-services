using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class Service
    {
        [JsonProperty("name", Required = Required.Always)]
        [OpenApiProperty(Description = "Name of the service")]
        public string? Name { get; set; }

        [JsonProperty("modules", Required = Required.Always)]
        [OpenApiProperty(Description = "List of available service modules")]
        public List<Module>? Modules { get; set; }
    }
}