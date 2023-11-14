using System.Collections.Generic;
using Newtonsoft.Json;

namespace Aire.Services.Models
{
    public class Service
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("modules")]
        public List<Module> Modules { get; set; }
    }
}