using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class Platform
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("modules")]
        public Dictionary<ModuleType, Module>? Modules { get; set; }
    }
}
