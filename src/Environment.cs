// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


namespace Aire.Services
{
    public static class AireEnvironment
    {
        public static string? StorageConnectionString => Environment.GetEnvironmentVariable("StorageConnectionString");
        public static string? PlatformConfiguration => Environment.GetEnvironmentVariable("PlatformConfiguration");
        public static string? TokenSigningKey => Environment.GetEnvironmentVariable("TOKEN_SIGNING_KEY");
        public static string? TokenEncryptionKey => Environment.GetEnvironmentVariable("TOKEN_ENCRYPTION_KEY");
        public static string? OpenApiHost => Environment.GetEnvironmentVariable("OpenApi__HostNames");
    }
}
