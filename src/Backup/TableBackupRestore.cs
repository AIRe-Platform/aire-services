// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Aire.Sdk.Azure;
using Microsoft.Extensions.Options;
using System.Web.Http;
using Azure.Data.Tables;

namespace Aire.Services.Backup;

public class TableBackupRestore
{
    private readonly ITableStorageService _storage;
    private readonly TableBackupOptions _options;
    private readonly ILogger<TableBackupRestore> _log;

    public TableBackupRestore(ITableStorageService storage, IOptions<TableBackupOptions> options, ILogger<TableBackupRestore> log)
    {
        _storage = storage;
        _options = options.Value;
        _log = log;
    }

    [Function("TableBackupRestore")]
    [OpenApiIgnore]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "backup/restore/{date_id}/{table_names?}")] HttpRequest req,
        string date_id,
        string table_names)
    {
        if (_options.IsValid())
        {
            _log.LogError("Invalid configuration");
            return new InternalServerErrorResult();
        }

        if (string.IsNullOrEmpty(date_id) || date_id.Length != 8)
        {
            _log.LogError("Invalid date_id");
            return new BadRequestResult();
        }

        // Backup destination as the source and vice versa!!
        var src = new TableServiceClient(_options.DestinationStorageConnectionString);
        var dst = new TableServiceClient(_options.SourceStorageConnectionString);

        var tables = table_names?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tables == null)
        {
            // Restore full backup
            var index = await _storage.Partition<TableBackupIndexEntity>(date_id);
            tables = index?.Select(x => x.TableName!).ToArray();
        }

        if (tables == null || tables.Length == 0)
        {
            _log.LogWarning("Nothing to restore");
            return new NoContentResult();
        }

        // Copy tables

        foreach (var table in tables!)
        {
            string input = $"{table}{date_id}";
            string output = $"{table}";

            _log.LogInformation("Restoring table '{table}' from backup '{date_id}'",
                output, date_id);

            var srcClient = src.GetTableClient(input);
            var dstClient = dst.GetTableClient(output);

            var result = await CopyTable(srcClient, dstClient);

            _log.LogInformation("Table '{table}' restored. Entities: {entities}. Failures: {failures}",
                output, result.Entities, result.Failures);
        }

        return new NoContentResult();
    }

    private static async Task<TableBackupResult> CopyTable(TableClient src, TableClient dst)
    {
        await dst.CreateIfNotExistsAsync();
        var query = src.QueryAsync<TableEntity>();

        int entities = 0;
        int failures = 0;

        await foreach (var ent in query)
        {
            var result = await dst.UpsertEntityAsync(ent);

            if (result.IsError)
                failures++;
            else
                entities++;
        }

        return new TableBackupResult { Entities = entities, Failures = failures };
    }
}
