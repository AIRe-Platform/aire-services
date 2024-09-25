// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


namespace Aire.Services.Backup;

public class TableBackupOptions
{
    public string? SourceStorageConnectionString { get; set; }
    public string? DestinationStorageConnectionString { get; set; }
    public TimeSpan? CleanUpOlderThan { get; set; }
    public string[]? Tables { get; set; }

    public bool IsValid()
    {
        return (
            Tables != null && 
            Tables.Length > 0 && 
            !string.IsNullOrWhiteSpace(SourceStorageConnectionString) && 
            !string.IsNullOrWhiteSpace(DestinationStorageConnectionString)
        );
    }
}
