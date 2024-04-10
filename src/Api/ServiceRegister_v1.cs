using System.Net;
using Aire.Sdk.AspNetCore;
using Aire.Sdk.Auth;
using Aire.Sdk.Azure;
using Aire.Sdk.Models.Platform;
using Aire.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Aire.Services.Api;

public class ServiceRegistration_v1
{
    private readonly IJwtTokenService _jwt;
    private readonly ITableStorageService _storage;
    private readonly ILogger<ServiceRegistration_v1> _log;

    public ServiceRegistration_v1(IJwtTokenService jwt, ITableStorageService storage, ILogger<ServiceRegistration_v1> log)
    {
        _jwt = jwt;
        _log = log;
        _storage = storage;
    }

    [Function("GetServiceList_v1")]
    [OpenApiOperation(
        operationId: "getServiceList",
        tags: ["Services"],
        Summary = "List of services",
        Description = "Returns public platform configuration object for public clients")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<Service>), Description = "List of services")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    public async Task<IActionResult> GetServiceList(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/service-register/services")] HttpRequest req,
        FunctionContext context)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.ReadServices))
            return new ForbiddenResult();

        List<Service> services;

        if (auth.Principal.IsInRole(AireRoles.Admin))
        {
            var q = await _storage.All<ServiceEntity>();
            services = q.Select(x => x.ToModel()).ToList();
        }
        else
        {
            var q = await _storage.QueryAsync<ServiceEntity>(x => x.Owner == auth.UserId);
            var list = await q.ToListAsync();
            services = list.Select(x => x.ToModel()).ToList();
        }

        return new OkObjectResult(services);
    }

    [Function("RegisterService_v1")]
    [OpenApiOperation(
        operationId: "registerService",
        tags: ["Services"],
        Summary = "Register a new service",
        Description = "Creates a new service object")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Service), Description = "Created service object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Conflict, Description = "A service with the name already exists")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> RegisterService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/service-register")] HttpRequest req,
        FunctionContext context)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.ReadServices))
            return new ForbiddenResult();

        var service = await req.ReadJson<Service>();
        if (service == null)
            return new BadRequestResult();

        string? config = AireEnvironment.PlatformConfiguration;
        if (config == null)
            throw new Exception("The platform is not configured");

        bool admin = auth.Principal.IsInRole(AireRoles.Admin);
        if (!admin && service.Owner != null && service.Owner != auth.UserId)
            return new ForbiddenResult();

        var entity = new ServiceEntity(config)
        {
            Name = service.Name,
            Owner = service.Owner ?? auth.UserId,
            Modules = service.Modules,
            Active = service.Active.GetValueOrDefault(false)
        };

        if (string.IsNullOrWhiteSpace(entity.Name) ||
            entity.Modules == null ||
            entity.Modules.Count < 1)
        {
            return new BadRequestResult();
        }

        var queryExisting = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == config && x.Name == entity.Name);
        var existing = await queryExisting.FirstOrDefaultAsync();
        if (existing != null)
            return new ConflictResult();

        var insert = await _storage.UpsertAsync(entity);
        if (!insert)
            throw new Exception("Failed to insert entity");

        return new OkObjectResult(entity.ToModel());
    }

    [Function("EditService_v1")]
    [OpenApiOperation(
        operationId: "editService",
        tags: ["Services"],
        Summary = "Edit a service",
        Description = "Returns public platform configuration object for public clients")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Service identifier")]
    [OpenApiRequestBody("application/json", typeof(Service), Required = true, Description = "Service object")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Service), Description = "Updated service object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "The service was not found")]
    public async Task<IActionResult> EditService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/service-register/{id}")] HttpRequest req,
        FunctionContext context,
        [FromRoute] string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.EditServices))
            return new ForbiddenResult();

        var data = await req.ReadJson<Service>();
        if (data == null || string.IsNullOrWhiteSpace(id) || data.Id != id)
            return new BadRequestResult();

        string? config = AireEnvironment.PlatformConfiguration;
        if (config == null)
            throw new Exception("The platform is not configured");

        var entity = await _storage.RetrieveAsync<ServiceEntity>(config, id);
        if (entity == null)
            return new NotFoundResult();

        if (data.Name != null)
            entity.Name = data.Name;

        if (data.Owner != null && data.Owner != entity.Owner)
        {
            if (!auth.Principal.IsInRole(AireRoles.Admin))
                return new ForbiddenResult();

            entity.Owner = data.Owner;
        }

        if (data.Modules != null)
            entity.Modules = data.Modules;

        if (data.Active.HasValue)
            entity.Active = data.Active.Value;

        var update = await _storage.UpsertAsync(entity);
        if (!update)
            throw new Exception("Failed to update entity");

        return new OkObjectResult(entity.ToModel());
    }

    [Function("DeleteService_v1")]
    [OpenApiOperation(
        operationId: "deleteService",
        tags: ["Services"],
        Summary = "Delete a service",
        Description = "Deletes the service registration")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("id", In = ParameterLocation.Path, Required = true, Description = "Service identifier")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Operation successful")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "The service was not found")]
    public async Task<IActionResult> DeleteService(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/service-register/{id}")] HttpRequest req,
        FunctionContext context,
        [FromRoute] string id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.DeleteServices))
            return new ForbiddenResult();

        if (string.IsNullOrWhiteSpace(id))
            return new BadRequestResult();

        string? config = AireEnvironment.PlatformConfiguration;
        if (config == null)
            throw new Exception("The platform is not configured");

        var entity = await _storage.RetrieveAsync<ServiceEntity>(config, id);
        if (entity == null)
            return new NotFoundResult();

        bool admin = auth.Principal.IsInRole(AireRoles.Admin);
        if (!admin && entity.Owner != auth.UserId)
            return new ForbiddenResult();

        var delete = await _storage.DeleteAsync(entity);
        if (!delete)
            throw new Exception("Failed to delete entity");

        return new NoContentResult();
    }
}
