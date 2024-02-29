using System.Runtime.Serialization;
using Aire.Sdk.Azure;
using Aire.Sdk.Helpers;
using Aire.Sdk.Models.Platform;

namespace Aire.Services.Models
{
    [EntityTable("Platform")]
    public class PlatformEntity : BaseTableEntity
    {
        public string? ConfigData { get; set; }

        [IgnoreDataMember]
        public PlatformConfiguration? Config {
            get => ConfigData?.JsonToObject<PlatformConfiguration>();
            set => ConfigData = value.ObjectToJson();
        }
    }
}
