using System.Runtime.Serialization;
using Aire.Sdk.Azure;
using Aire.Sdk.Helpers;
using Aire.Sdk.Models.Platform;

namespace Aire.Services.Models
{
    /// <summary>
    /// Platform configuration name: PartitionKey
    /// Service ID: RowKey
    /// </summary>
    [EntityTable("Services")]
    public class ServiceEntity : BaseTableEntity
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public string? ModuleData { get; set; }

        [IgnoreDataMember]
        public List<Module>? Modules
        {
            get => ModuleData?.JsonToObject<List<Module>>();
            set => ModuleData = value.ObjectToJson();
        }

        public ServiceEntity() { }
        public ServiceEntity(string platform_config_name)
        {
            PartitionKey = platform_config_name;
            RowKey = Guid.NewGuid().ToString();
        }

        public string PlatformConfig()
        {
            return PartitionKey ?? "";
        }

        public string Id()
        {
            return RowKey ?? "";
        }

        public Service ToModel()
        {
            return new Service()
            {
                Id = Id(),
                Name = Name,
                Owner = Owner,
                Modules = Modules
            };
        }
    }
}
