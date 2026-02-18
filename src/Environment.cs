// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


namespace Aire.Services
{
    public static class AireServicesEnvironment
    {
        public static string? StorageConnectionString => Environment.GetEnvironmentVariable("StorageConnectionString");
        public static string? OpenApiHost => Environment.GetEnvironmentVariable("OpenApi__HostNames");
        public static string? BackupExpiryDays => Environment.GetEnvironmentVariable("BackupExpiryDays");
        public static string? BackupStorageConnectionString => Environment.GetEnvironmentVariable("BackupStorageConnectionString");
        public static string? BackupTables => Environment.GetEnvironmentVariable("BackupTables");
    }
}
