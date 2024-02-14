using Azure;
using Azure.Data.Tables;

namespace Aire.Services.Models
{
    public class BaseTableEntity : ITableEntity
    {
        public virtual string? PartitionKey { get; set; }
        public virtual string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
