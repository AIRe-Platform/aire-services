using System.Net;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Aire.Sdk.Azure;
using Aire.Services.Models;
using Aire.Sdk.Models.Platform;
using Aire.Sdk.Auth.Extensions;

namespace Aire.Services.Api;

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
        operationId: "getConfig",
        tags: ["configuration"],
        Summary = "Public platform configuration",
        Description = "Returns public platform configuration object for public clients")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "The platform is not configured properly")]
    public async Task<IActionResult> GetConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config")] HttpRequest req)
    {
        var entity = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (entity == null)
        {
            _log.LogError("Default platform not configured!");
            return new InternalServerErrorResult();
        }

        var config = entity.Config;

        // Remove services that require service-to-service authentication
        foreach (var svc in config!.Services!)
        {
            svc.Modules = svc.Modules!
                .Where(x => x.Access != ModuleAccess.Service)
                .ToList();
        }

        return new OkObjectResult(config);
    }

    [Function("GetConfigInternal_v1")]
    [OpenApiOperation(
        operationId: "getConfigInternal",
        tags: ["configuration"],
        Summary = "Internal platform configuration",
        Description = "Returns internal platform configuration object for internal services")]
    [OpenApiSecurity(
        schemeName: "AireServiceKey",
        schemeType: SecuritySchemeType.ApiKey,
        Name = "Aire-Service-Key",
        In = OpenApiSecurityLocationType.Header,
        Description = "Internal platform module service key")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid service key")]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "The platform is not configured properly")]
    public async Task<IActionResult> GetConfigInternal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config/internal")] HttpRequest req)
    {
        if (!req.IsServiceRequest())
            return new UnauthorizedResult();

        var entity = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (entity == null)
        {
            _log.LogError("Default platform not configured!");
            return new InternalServerErrorResult();
        }

        return new OkObjectResult(entity.Config);
    }
}
