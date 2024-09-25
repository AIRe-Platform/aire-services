// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Aire.Sdk.Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aire.Services.Backup;

public class TableBackupActivity
{
    private readonly ITableStorageService _storage;
    private readonly TableBackupOptions _options;
    private readonly ILogger<TableBackupActivity> _log;

    private readonly TableServiceClient _srcClient;
    private readonly TableServiceClient _dstClient;

    public TableBackupActivity(ITableStorageService storage, IOptions<TableBackupOptions> options, ILogger<TableBackupActivity> log)
    {
        _storage = storage;
        _options = options.Value;
        _log = log;

        _srcClient = new TableServiceClient(_options.SourceStorageConnectionString);
        _dstClient = new TableServiceClient(_options.DestinationStorageConnectionString);
    }

    [Function(nameof(TableBackupActivity))]
    public async Task<TableBackupResult> BackupTable([ActivityTrigger] string input)
    {
        _log.LogInformation("Backing up table '{table}'...", input);
        int count = 0;
        int failures = 0;

        var now = DateTime.UtcNow;
        string output = $"{input}{now:yyyyMMdd}";

        await _dstClient.CreateTableIfNotExistsAsync(output);

        var src = _srcClient.GetTableClient(input);
        var dst = _dstClient.GetTableClient(output);

        var query = src.QueryAsync<TableEntity>();
        await foreach (var e in query)
        {
            var res = await dst.UpsertEntityAsync(e, TableUpdateMode.Replace);
            if (res.IsError)
                failures++;
            else
                count++;
        }

        _log.LogInformation("Finished backing up table '{table}'. Copied entities: {count}. Failures: {errors}.",
            input, count, failures);

        var results = new TableBackupResult
        {
            Entities = count,
            Failures = failures
        };

        _log.LogInformation("Generating BackupIndex entry...");
        var entry = new TableBackupIndexEntity()
        {
            DateId = DateTime.UtcNow.ToString("yyyyMMdd"),
            TableName = input,
            Result = results
        };

        var upsert = await _storage.UpsertAsync(entry);
        if (!upsert)
        {
            _log.LogError("Failed to upsert index entity");
        }

        return results;
    }
}
