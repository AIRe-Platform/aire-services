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
        public string? AgentConfig { get; set; }

        [IgnoreDataMember]
        public Platform? Platform
        {
            get => ConfigData?.JsonToObject<Platform>() ?? null;
            set => ConfigData = value.ObjectToJson();
        }

        [IgnoreDataMember]
        public InstanceSettings Settings
        {
            get => InstanceSettings?.JsonToObject<InstanceSettings>() ?? _defaultInstanceSettings;
            set => InstanceSettings = value.ObjectToJson();
        }

        [IgnoreDataMember]
        public List<AgentConfig> Agents
        {
            get => AgentConfig?.JsonToObject<List<AgentConfig>>() ?? [];
            set => AgentConfig = value.ObjectToJson();
        }

        private static readonly InstanceSettings _defaultInstanceSettings = new() { InactivityDuration = 30 };
    }
}
