// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Aire.Services.Backup;

public static class TableBackupOrchestration
{
    [Function(nameof(TableBackupOrchestration))]
    public static async Task<Dictionary<string, TableBackupResult>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context, TableBackupOptions input)
    {
        if (!input.IsValid())
            throw new InvalidOperationException("Invalid configuration");

        ILogger logger = context.CreateReplaySafeLogger(nameof(TableBackupOrchestration));
        logger.LogInformation("Starting backup orchestration");
        var tasks = new Dictionary<string, Task<TableBackupResult>>();

        foreach (var table in input.Tables!)
        {
            logger.LogInformation("Calling backup activity for table '{table}'...", table);
            var task = context.CallActivityAsync<TableBackupResult>(nameof(TableBackupActivity), table);
            tasks.Add(table, task);
        }

        await Task.WhenAll(tasks.Values);
        var completedTasks = tasks.Where(x => x.Value.IsCompletedSuccessfully);
        var failedTasks = tasks.Where(x => !x.Value.IsCompletedSuccessfully);

        foreach (var failed in failedTasks)
        {
            logger.LogError("Failed to backup table '{table}'", failed.Key);
        }

        logger.LogInformation("Finished backup orchestration");
        var results = completedTasks.ToDictionary(x => x.Key, x => x.Value.Result);
        return results;
    }
}
