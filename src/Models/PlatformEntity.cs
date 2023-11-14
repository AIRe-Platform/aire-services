using System.Runtime.Serialization;
using Aire.Helpers;

namespace Aire.Services.Models
{
    [EntityTable("Platform")]
    public class PlatformEntity : BaseTableEntity
    {
        public string ConfigData { get; set; }

        [IgnoreDataMember]
        public PlatformConfiguration Config {
            get => ConfigData.JsonToObject<PlatformConfiguration>();
            set => ConfigData = value.ObjectToJson();
        }
    }
}