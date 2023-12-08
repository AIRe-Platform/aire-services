using System.Linq.Expressions;
using System.Reflection;
using Azure;
using Azure.Data.Tables;
using Aire.Helpers;

namespace Aire.Services
{
    public class TableStorageService : ITableStorageService
    {
        private readonly TableServiceClient svcClient;

        public TableStorageService()
        {
            svcClient = new TableServiceClient(AireEnvironment.StorageConnectionString);
        }

        private static string? GetEntityTableName(Type t)
        {
            var attr = t.GetCustomAttribute(typeof(EntityTableAttribute));
            if(attr == null)
                return null;
            return ((EntityTableAttribute) attr).TableName;
        }

        private async Task<TableClient> GetTableClientAsync(Type t)
        {
            var tableName = GetEntityTableName(t);
            
            if(string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    $"The table entity of type '{t}' is missing attribute '{nameof(EntityTableAttribute)}'!", 
                    nameof(t));
            }

            var table = svcClient.GetTableClient(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<T?> RetrieveAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var client = await GetTableClientAsync(typeof(T));
            var response = await client.GetEntityIfExistsAsync<T>(partitionKey, rowKey);

            if(response.HasValue)
                return response.Value;

            return null;
        }

        public async Task<bool> UpsertAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            var client = await GetTableClientAsync(typeof(T));
            var response = await client.UpsertEntityAsync<T>(entity, TableUpdateMode.Replace);
            return !response.IsError;
        }

        public async Task<bool> DeleteAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            return await DeleteAsync<T>(entity.PartitionKey, entity.RowKey);
        }

        public async Task<bool> DeleteAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var client = await GetTableClientAsync(typeof(T));
            var response = await client.DeleteEntityAsync(partitionKey, rowKey);
            return !response.IsError;
        }

        public async Task<AsyncPageable<T>> QueryAsync<T>(Expression<Func<T, bool>> expression) where T : class, ITableEntity, new()
        {
            var client = await GetTableClientAsync(typeof(T));
            return client.QueryAsync(expression);
        }
    }
}