using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;

namespace Aire.Helpers
{
    public interface ITableStorageService 
    {
        Task<T> RetrieveAsync<T>(string partitionKey, string rowKey) 
            where T: class, ITableEntity, new();

        Task<bool> UpsertAsync<T>(T entity)
            where T: class, ITableEntity, new();
            
        Task<bool> DeleteAsync<T>(T entity)
            where T: class, ITableEntity, new();

        Task<bool> DeleteAsync<T>(string partitionKey, string rowKey)
            where T: class, ITableEntity, new();

        Task<AsyncPageable<T>> QueryAsync<T>(Expression<Func<T, bool>> expression)
            where T: class, ITableEntity, new();
    }
}