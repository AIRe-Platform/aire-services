// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Aire.Sdk.Azure;
using Aire.Services.Models;
using Aire.Sdk.Models.Platform;
using Aire.Sdk.Auth.Extensions;
using Aire.Sdk.Auth;
using Aire.Sdk.AspNetCore;

namespace Aire.Services.Api;

public class Config_v1(IJwtTokenService _jwt, ITableStorageService _storage)
{
    [Function("GetPublicConfig_v1")]
    [OpenApiOperation("getPublicConfig", ["Configuration"], Summary = "Public platform configuration")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Configuration identifier")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "Configuration error")]
    public async Task<IActionResult> GetPublicConfig(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config/{id}/public")] HttpRequest req,
        string id)
    {
        var entity = await _storage.RetrieveAsync<PlatformEntity>(id, AireConstants.PlatformConfigRowKey);
        if (entity == null)
            return new NotFoundResult();

        if (entity.Platform == null)
            throw new Exception("Default platform not configured!");

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
            .Where(x => x.Modules != null && x.Modules.Count > 0 && x.Active == true)
            .ToList();

        var platform = entity.Platform;
        if (platform.Modules != null)
        {
            foreach (var modlist in platform.Modules)
            {
                foreach (var mod in modlist.Value)
                {
                    // Module settings are internal only
                    mod.Settings = null;
                }
            }
        }

        var agents = (entity.Agents ?? []).Select(x =>
        {
            // Hide LLM configuration from public
            x.Prompt = null;
            x.Tools = null;
            return x;
        });

        var config = new PlatformConfiguration
        {
            Platform = platform,
            Services = services,
            Settings = entity.Settings,
            Agents = [.. agents]
        };

        return new OkObjectResult(config);
    }

    [Function("GetConfigInternal_v1")]
    [OpenApiOperation("getConfigInternal", ["Configuration"], Summary = "Internal platform configuration")]
    [OpenApiSecurity("AireServiceKey", SecuritySchemeType.ApiKey,
        Name = "Aire-Service-Key",
        In = OpenApiSecurityLocationType.Header,
        Description = "Internal platform module service key")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http,
        Scheme = OpenApiSecuritySchemeType.Bearer,
        BearerFormat = "JWT",
        Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Configuration identifier")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PlatformConfiguration), Description = "Platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid token or service key")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError, Description = "Configuration error")]
    public async Task<IActionResult> GetConfigInternal(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config/{id}/internal")] HttpRequest req,
        FunctionContext context,
        string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
        {
            if (!req.IsServiceRequest())
                return new UnauthorizedResult();
        }
        else if (!_jwt.CheckAuthorization(auth, AireScopes.AdminConfig))
            return new ForbiddenResult();

        var entity = await _storage.RetrieveAsync<PlatformEntity>(id, AireConstants.PlatformConfigRowKey);
        if (entity == null)
            return new NotFoundResult();

        if (entity.Platform == null)
            throw new Exception("Default platform not configured!");

        var serviceQuery = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == entity.PartitionKey);
        var serviceList = await serviceQuery.ToListAsync();
        var services = serviceList.Where(x => x.Active).Select(x => x.ToModel()).ToList();

        var config = new PlatformConfiguration
        {
            Platform = entity.Platform,
            Services = services,
            Settings = entity.Settings,
            Agents = entity.Agents
        };

        return new OkObjectResult(config);
    }

    [Function("ListConfigurations_v1")]
    [OpenApiOperation("listConfigurations", ["Configuration"], Summary = "List platform configurations")]
    [OpenApiSecurity("AireServiceKey", SecuritySchemeType.ApiKey, Name = "Aire-Service-Key", In = OpenApiSecurityLocationType.Header, Description = "Internal platform module service key")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Dictionary<string, PlatformConfiguration>), Description = "Platform configurations by ID")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid token or service key")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    public async Task<IActionResult> ListConfigurations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/configs")] HttpRequest req,
        FunctionContext context)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
        {
            if (!req.IsServiceRequest())
                return new UnauthorizedResult();
        }
        else if (!_jwt.CheckAuthorization(auth, AireScopes.AdminConfig))
            return new ForbiddenResult();

        var results = new Dictionary<string, PlatformConfiguration>();
        var entities = await _storage.All<PlatformEntity>();

        foreach (var entity in entities)
        {
            var serviceQuery = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == entity.PartitionKey);
            var serviceList = await serviceQuery.ToListAsync();
            var services = serviceList.Where(x => x.Active).Select(x => x.ToModel()).ToList();

            var config = new PlatformConfiguration
            {
                Platform = entity.Platform,
                Services = services,
                Settings = entity.Settings,
                Agents = entity.Agents
            };

            results.Add(entity.PartitionKey!, config);
        }

        return new OkObjectResult(results);
    }

    [Function("CreateConfiguration_v1")]
    [OpenApiOperation("createConfiguration", ["Configuration"], Summary = "Create platform configuration")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Configuration identifier")]
    [OpenApiRequestBody("application/json", typeof(Platform), Required = true, Description = "Platform configuration")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Platform), Description = "Platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid token")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.UnprocessableEntity, Description = "Failed to parse request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Conflict, Description = "Already exists")]
    public async Task<IActionResult> CreateConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/config/{id}")] HttpRequest req,
        FunctionContext context,
        string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminConfig))
            return new ForbiddenResult();

        var config = await req.ReadJson<Platform>();
        if (config == null)
            return new UnprocessableEntityResult();

        var entity = await _storage.RetrieveAsync<PlatformEntity>(id, AireConstants.PlatformConfigRowKey);
        if (entity != null)
            return new ConflictResult();

        entity = new PlatformEntity
        {
            PartitionKey = id,
            RowKey = AireConstants.PlatformConfigRowKey,
            Platform = config,
        };

        bool updated = await _storage.UpsertAsync(entity);
        if (!updated)
            throw new Exception("Failed to create platform configuration");

        return new OkObjectResult(entity.Platform);
    }

    [Function("EditConfiguration_v1")]
    [OpenApiOperation("editConfiguration", ["Configuration"], Summary = "Edit platform configuration")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Configuration identifier")]
    [OpenApiRequestBody("application/json", typeof(Platform), Required = true, Description = "Platform configuration")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Platform), Description = "Updated platform configuration")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid token")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.UnprocessableEntity, Description = "Failed to parse request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    public async Task<IActionResult> EditConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/config/{id}")] HttpRequest req,
        FunctionContext context,
        string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminConfig))
            return new ForbiddenResult();

        var config = await req.ReadJson<Platform>();
        if (config == null)
            return new UnprocessableEntityResult();

        var entity = await _storage.RetrieveAsync<PlatformEntity>(id, AireConstants.PlatformConfigRowKey);
        if (entity == null)
            return new NotFoundResult();

        entity.Platform = config;
        bool updated = await _storage.UpsertAsync(entity);
        if (!updated)
            throw new Exception("Failed to update platform configuration");

        return new OkObjectResult(entity.Platform);
    }

    [Function("DeleteConfiguration_v1")]
    [OpenApiOperation("deleteConfiguration", ["Configuration"], Summary = "Delete platform configuration")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Configuration identifier")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Configuration deleted")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid token")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    public async Task<IActionResult> DeleteConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/config/{id}")] HttpRequest req,
        FunctionContext context,
        string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminConfig))
            return new ForbiddenResult();

        var entity = await _storage.RetrieveAsync<PlatformEntity>(id, AireConstants.PlatformConfigRowKey);
        if (entity == null)
            return new NotFoundResult();

        bool updated = await _storage.DeleteAsync(entity);
        if (!updated)
            throw new Exception("Failed to delete platform configuration");

        return new NoContentResult();
    }
}
