// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Aire.Sdk.Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aire.Services.Backup;

public class TableBackupTimer
{
    private readonly ITableStorageService _storage;
    private readonly ILogger<TableBackupTimer> _log;
    private readonly TableBackupOptions _config;

    public TableBackupTimer(ITableStorageService storage, ILogger<TableBackupTimer> log, IOptions<TableBackupOptions> config)
    {
        _storage = storage;
        _log = log;
        _config = config.Value;
    }

    [Function("TableBackupTimer")]
    public async Task ScheduleBackup(
        [TimerTrigger("0 0 1 * * 0")] TimerInfo timer, // Run at every Sunday 01:00 UTC
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        if (_config == null)
        {
            _log.LogWarning("BackupOptions is not configured. Skipping.");
            return;
        }

        if (!_config.IsValid())
        {
            _log.LogWarning("Invalid BackupOptions. Skipping.");
            return;
        }

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(TableBackupOrchestration), _config);
        _log.LogInformation("Started backup orchestration with ID = '{instanceId}'.", instanceId);
    }

    [Function("TableBackupCleanupTimer")]
    public async Task CleanupBackups(
        [TimerTrigger("0 0 0 * * 0")] TimerInfo timer, // Run at every Sunday 00:00 UTC
        FunctionContext executionContext)
    {
        if (_config == null)
        {
            _log.LogWarning("BackupOptions is not configured. Skipping.");
            return;
        }

        if (!_config.IsValid())
        {
            _log.LogWarning("Invalid BackupOptions. Skipping.");
            return;
        }

        if (!_config.CleanUpOlderThan.HasValue)
        {
            _log.LogInformation("Backup clean up disabled. Skipping.");
            return;
        }

        _log.LogInformation("Starting to clean up old back ups");

        var backupStorage = new TableServiceClient(_config.DestinationStorageConnectionString);
        var maxAge = DateTime.UtcNow - _config.CleanUpOlderThan;
        var index = await _storage.QueryAsync<TableBackupIndexEntity>(x => x.Timestamp < maxAge);

        await foreach(var e in index)
        {
            string table = $"{e.TableName}{e.DateId}";
            await backupStorage.DeleteTableAsync(table);
            await _storage.DeleteAsync(e);
        }

        _log.LogInformation("Finished cleaning up old backups");
    }
}
