using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aire.Services.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModuleType
    {
        [EnumMember(Value = "id")]
        ID,

        [EnumMember(Value = "ai")]
        AI,

        [EnumMember(Value = "memory")]
        Memory
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModuleAccess
    {
        // Public modules are read-only
        // These can be public databases containing generic data
        [EnumMember(Value = "public")]
        Public,

        // Private modules require that users connect with third-party ID providers
        // These modules can be used to store user data
        [EnumMember(Value = "private")]
        Private
    }

    public class Module
    {
        [JsonProperty("type")]
        public ModuleType Type { get; set; }

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("access")]
        public ModuleAccess Access { get; set; }
    }
}
