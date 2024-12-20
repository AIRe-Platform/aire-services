// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
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
using Microsoft.OpenApi.Models;

namespace Aire.Services.Api;

public class InstanceSettings_v1(IJwtTokenService _jwt, ITableStorageService _storage)
{
    [Function("EditInstanceSettings_v1")]
    [OpenApiOperation(
        operationId: "editInstanceSettings",
        tags: ["Settings"],
        Summary = "Edit instance settings",
        Description = "Edit general instance settings")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiRequestBody("application/json", typeof(InstanceSettings), Description = "Instance settings object")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(InstanceSettings), Description = "Edited instance settings object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> EditInstanceSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/settings/instance")] HttpRequest req,
        FunctionContext context)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminInstanceSettings))
            return new ForbiddenResult();

        var platform = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (platform?.Platform == null)
            throw new Exception("Default platform not configured!");

        var instanceSettings = platform.Settings;

        var settings = await req.ReadJson<InstanceSettings>();
        if (settings == null)
            return new BadRequestResult();

        if (settings.InactivityDuration.HasValue)
        {
            var validationContext = new ValidationContext(settings)
            {
                MemberName = nameof(settings.InactivityDuration)
            };

            var validationResults = new List<ValidationResult>();
            bool pass = Validator.TryValidateProperty(settings.InactivityDuration, validationContext, validationResults);
            if (!pass)
                return new BadRequestObjectResult(validationResults);

            instanceSettings.InactivityDuration = settings.InactivityDuration.Value;
        }

        platform.Settings = instanceSettings;
        bool updated = await _storage.UpsertAsync(platform);
        if (!updated)
            throw new Exception("Failed to update platform instance settings");

        return new OkObjectResult(instanceSettings);
    }

    [Function("GetModuleInstanceSettings_v1")]
    [OpenApiOperation(
        operationId: "getModuleInstanceSettings",
        tags: ["Settings"],
        Summary = "Get module instance settings",
        Description = "Get module instance settings")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("module", Description = "Module type", Required = true, In = ParameterLocation.Path)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Dictionary<string, dynamic>), Description = "Module settings dictionary")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Module settings not found")]
    public async Task<IActionResult> GetModuleInstanceSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/settings/{module}")] HttpRequest req,
        FunctionContext context,
        string module)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminInstanceSettings))
            return new ForbiddenResult();

        var platformEntity = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (platformEntity?.Platform?.Modules == null)
            throw new Exception("Default platform not configured!");

        ModuleType moduleType;
        switch (module)
        {
            case "ai": moduleType = ModuleType.AI; break;
            case "memory": moduleType = ModuleType.ID; break;
            case "id": moduleType = ModuleType.Memory; break;
            default:
                return new NotFoundResult();
        }

        if (!platformEntity.Platform.Modules.ContainsKey(moduleType))
            return new NotFoundResult();

        var settings = platformEntity.Platform.Modules[moduleType].Settings ?? [];
        return new OkObjectResult(settings);
    }

    [Function("EditModuleInstanceSettings_v1")]
    [OpenApiOperation(
        operationId: "editModuleInstanceSettings",
        tags: ["Settings"],
        Summary = "Edit module instance settings",
        Description = "Edit module instance settings")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("module", Description = "Module type", Required = true, In = ParameterLocation.Path)]
    [OpenApiRequestBody("application/json", typeof(Dictionary<string, dynamic>), Description = "Module settings dictionary")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Dictionary<string, dynamic>), Description = "Updated module settings dictionary")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Module not found")]
    public async Task<IActionResult> EditModuleSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/settings/{module}")] HttpRequest req,
        FunctionContext context,
        string module)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminInstanceSettings))
            return new ForbiddenResult();

        var platformEntity = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (platformEntity?.Platform?.Modules == null)
            throw new Exception("Default platform not configured!");

        var settings = await req.ReadJson<Dictionary<string, dynamic>>();
        if (settings == null)
            return new BadRequestResult();

        ModuleType moduleType;
        switch (module)
        {
            case "ai": moduleType = ModuleType.AI; break;
            case "memory": moduleType = ModuleType.ID; break;
            case "id": moduleType = ModuleType.Memory; break;
            default:
                return new NotFoundResult();
        }

        var platform = platformEntity.Platform;
        if (!platform.Modules.ContainsKey(moduleType))
            return new NotFoundResult();

        platform.Modules[moduleType].Settings = settings;
        platformEntity.Platform = platform;

        bool updated = await _storage.UpsertAsync(platformEntity);
        if (!updated)
            throw new Exception("Failed to update module settings");

        return new OkObjectResult(settings);
    }
}
