using System.Runtime.Serialization;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
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

        // Public modules that require service-to-service authentication
        // These cannot be accessed by public clients
        [EnumMember(Value = "service")]
        Service,

        // Private modules require that users connect with third-party ID providers
        // These modules can be used to store user data
        [EnumMember(Value = "private")]
        Private
    }

    public class Module
    {
        [JsonProperty("type", Required = Required.Always)]
        [OpenApiProperty(Description = "Type of the module")]
        public ModuleType Type { get; set; }

        [JsonProperty("endpoint", Required = Required.Always)]
        [OpenApiProperty(Description = "Module endpoint root")]
        public string? Endpoint { get; set; }

        [JsonProperty("access", Required = Required.Always)]
        [OpenApiProperty(Description = """
        Required access level.
        Public modules do not generally require authentication.
        Service modules can only be accessed from other modules with server-to-server authentication.
        Private modules require the user to be logger in.
        """)]
        public ModuleAccess Access { get; set; }

        [JsonProperty("credentials", NullValueHandling = NullValueHandling.Ignore)]
        [OpenApiProperty(Description = "Server-to-server client credentials")]
        public ClientCredentials? Credentials { get; set; } = null;
    }
}
