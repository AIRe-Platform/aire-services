namespace Aire.Services
{
    public static class AireEnvironment
    {
        public static string? StorageConnectionString => Environment.GetEnvironmentVariable("StorageConnectionString");
        public static string? PlatformConfiguration => Environment.GetEnvironmentVariable("PlatformConfiguration");
        public static string? TokenSigningKey => Environment.GetEnvironmentVariable("TokenSigningKey");
        public static string? TokenEncryptionKey => Environment.GetEnvironmentVariable("TokenEncryptionKey");
        public static string? OpenApiHost => Environment.GetEnvironmentVariable("OpenApi__HostNames");
    }
}
