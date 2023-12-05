using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.Functions.Worker;
using Aire.Helpers;
using Aire.Services.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace Aire.Services.Api
{
    public class Config_v1
    {
        private readonly ILogger<Config_v1> _log;
        private readonly ITableStorageService _storage;

        public Config_v1(ILogger<Config_v1> log, ITableStorageService storage)
        {
            _log = log;
            _storage = storage;
        }

        [Function("GetConfig_v1")]
        [OpenApiOperation(
            operationId: "GetConfig", 
            tags: new[] { "Configuration" },
            Description = "Returns public platform configuration object")]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Occurs when the platform is not configured")]
        public async Task<IActionResult> GetConfig(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config")] HttpRequest req)
        {
            var entity = await _storage.RetrieveAsync<PlatformEntity>(
                AireEnvironment.PlatformConfiguration, 
                AireConstants.PlatformConfigRowKey);
            
            if(entity == null)
            {
                _log.LogError("Default platform not configured!");
                return new NotFoundResult();
            }

            var config = entity.Config;

            // Remove services that require service-to-service authentication
            foreach(var svc in config.Services)
            {
                svc.Modules = svc.Modules
                    .Where(x => x.Access != ModuleAccess.Service)
                    .ToList();
            }

            return new OkObjectResult(config);
        }

        [Function("GetConfigInternal_v1")]
        [OpenApiOperation(
            operationId: "GetConfigInternal", 
            tags: new[] { "Configuration" },
            Description = "Returns internal platform configuration object")]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
        [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Occurs when the platform is not configured")]
        public async Task<IActionResult> GetConfigInternal(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/config/internal")] HttpRequest req)
        {
            var entity = await _storage.RetrieveAsync<PlatformEntity>(
                AireEnvironment.PlatformConfiguration, 
                AireConstants.PlatformConfigRowKey);
            
            if(entity == null)
            {
                _log.LogError("Default platform not configured!");
                return new NotFoundResult();
            }

            return new OkObjectResult(entity.Config);
        }
    }
}

