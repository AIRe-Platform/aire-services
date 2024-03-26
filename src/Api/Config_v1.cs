using System.Net;
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
    private readonly ITableStorageService _storage;
    private readonly ILogger<Config_v1> _log;

    public Config_v1(ITableStorageService storage, ILogger<Config_v1> log)
    {
        _storage = storage;
        _log = log;
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

        if (entity == null || entity.Platform == null)
        {
            throw new Exception("Default platform not configured!");
        }

        var serviceQuery = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == entity.PartitionKey);
        var serviceList = await serviceQuery.ToListAsync();
        var services = serviceList.Select(x => x.ToModel()).ToList();

        // Only public modules:
        // Remove services that require service-to-service authentication
        foreach (var svc in services)
        {
            svc.Modules = svc.Modules?
                .Where(x => x.Access != ModuleAccess.Service)
                .ToList();
        }

        // Delist services with no available modules
        services = services
            .Where(x => x.Modules != null && x.Modules.Count > 0)
            .ToList();

        var config = new PlatformConfiguration
        {
            Platform = entity.Platform,
            Services = services
        };

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

        if (entity == null || entity.Platform == null)
        {
            throw new Exception("Default platform not configured!");
        }

        var serviceQuery = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == entity.PartitionKey);
        var serviceList = await serviceQuery.ToListAsync();
        var services = serviceList.Select(x => x.ToModel()).ToList();

        var config = new PlatformConfiguration
        {
            Platform = entity.Platform,
            Services = services
        };

        return new OkObjectResult(config);
    }
}
