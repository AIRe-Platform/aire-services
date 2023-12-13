using System.Runtime.Serialization;
using Aire.Sdk.TableStorage;
using Aire.Sdk.Helpers;

namespace Aire.Services.Models
{
    [EntityTable("Services")]
    public class ServiceEntity : BaseTableEntity
    {
        public string? ServiceData { get; set; }

        [IgnoreDataMember]
        public Service? Service {
            get => ServiceData?.JsonToObject<Service>();
            set => ServiceData = value.ObjectToJson();
        }
    }
}