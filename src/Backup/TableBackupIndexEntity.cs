using System.Runtime.Serialization;
using Aire.Sdk.Azure;
using Aire.Sdk.Helpers;

namespace Aire.Services.Backup;

[EntityTable("BackupIndex")]
public class TableBackupIndexEntity : BaseTableEntity
{
    [IgnoreDataMember]
    public string? DateId {
        get => PartitionKey;
        set => PartitionKey = value;
    }

    [IgnoreDataMember]
    public string? TableName {
        get => RowKey;
        set => RowKey = value;
    }

    [IgnoreDataMember]
    public TableBackupResult? Result {
        get => ResultData?.JsonToObject<TableBackupResult>();
        set => ResultData = value.ObjectToJson();
    }

    public string? ResultData { get; set; }
}