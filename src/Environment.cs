namespace Aire.Services
{
    public static class AireEnvironment
    {
        public static string StorageConnectionString {
            get => System.Environment.GetEnvironmentVariable("StorageConnectionString");
        }

        public static string IDModuleEndpoint { 
            get => System.Environment.GetEnvironmentVariable("ID_MODULE_ENDPOINT");
        }

        public static string AIModuleEndpoint {
            get => System.Environment.GetEnvironmentVariable("AI_MODULE_ENDPOINT");
        }

        public static string MemoryModuleEndpoint {
            get => System.Environment.GetEnvironmentVariable("MEMORY_MODULE_ENDPOINT");
        }

        public static string PlatformConfiguration {
            get => System.Environment.GetEnvironmentVariable("PLATFORM_CONFIGURATION");
        }
    }
}