// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using System.Runtime.Serialization;
using Aire.Sdk.Azure;
using Aire.Sdk.Helpers;
using Aire.Sdk.Models.Platform;

namespace Aire.Services.Models
{
    /// <summary>
    /// Configuration name: PartitionKey
    /// Configuration type: RowKey (default: "platform")
    /// </summary>
    [EntityTable("Platform")]
    public class PlatformEntity : BaseTableEntity
    {
        public string? ConfigData { get; set; }
        public string? InstanceSettings { get; set; }

        [IgnoreDataMember]
        public Platform? Platform
        {
            get
            {
                var platform = ConfigData?.JsonToObject<Platform>();

                if (platform?.Modules == null)
                {
                    var config = ConfigData?.JsonToObject<PlatformConfiguration>();
                    platform = config?.Platform;
                }
                if (platform is not null)
                {
                    platform.Settings = Settings;
                }
                return platform;
            }
            set => ConfigData = value.ObjectToJson();
        }

        [IgnoreDataMember]
        public InstanceSettings Settings
        {
            get => InstanceSettings?.JsonToObject<InstanceSettings>() ?? _defaultInstanceSettings;
            set => InstanceSettings = value.ObjectToJson();
        }

        private static readonly InstanceSettings _defaultInstanceSettings = new() { InactivityDuration = 30 };
    }
}
