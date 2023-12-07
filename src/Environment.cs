namespace Aire.Services
{
    public static class AireEnvironment
    {
        public static string StorageConnectionString {
            get => System.Environment.GetEnvironmentVariable("StorageConnectionString");
        }

        public static string PlatformConfiguration {
            get => System.Environment.GetEnvironmentVariable("PlatformConfiguration");
        }

        public static string TokenSigningKey {
            get => System.Environment.GetEnvironmentVariable("TokenSigningKey");
        }

        public static string TokenEncryptionKey {
            get => System.Environment.GetEnvironmentVariable("TokenEncryptionKey");
        }
    }
}
