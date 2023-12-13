using System.Runtime.Serialization;
using Aire.Sdk.TableStorage;
using Aire.Sdk.Helpers;

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