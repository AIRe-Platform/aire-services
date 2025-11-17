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

public class Settings_v1(IJwtTokenService _jwt, ITableStorageService _storage)
{
    [Function("EditInstanceSettings_v1")]
    [OpenApiOperation("editInstanceSettings", ["Settings"], Summary = "Edit instance settings")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiRequestBody("application/json", typeof(InstanceSettings), Description = "Instance settings object")]
    [OpenApiParameter("config_id", Description = "Configuration identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(InstanceSettings), Description = "Edited instance settings object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.UnprocessableEntity, Description = "Failed to parse request")]
    public async Task<IActionResult> EditInstanceSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/config/{config_id}/settings")] HttpRequest req,
        FunctionContext context,
        string config_id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminInstanceSettings))
            return new ForbiddenResult();

        var platform = await _storage.RetrieveAsync<PlatformEntity>(config_id, AireConstants.PlatformConfigRowKey);
        if (platform == null)
            return new NotFoundResult();

        if (platform.Platform == null)
            throw new Exception("Default platform not configured!");

        var instanceSettings = platform.Settings;

        var settings = await req.ReadJson<InstanceSettings>();
        if (settings == null)
            return new UnprocessableEntityResult();

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

    [Function("EditModuleSettings_v1")]
    [OpenApiOperation("editModuleSettings", ["Settings"], Summary = "Edit module settings")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiRequestBody("application/json", typeof(Dictionary<string, dynamic>), Description = "Module settings object")]
    [OpenApiParameter("config_id", Description = "Configuration identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiParameter("module_type", Description = "Module type", Required = true, In = ParameterLocation.Path)]
    [OpenApiParameter("module_id", Description = "Module identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Dictionary<string, dynamic>), Description = "Edited instance settings object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration or module not found")]
    [OpenApiResponseWithoutBody(HttpStatusCode.UnprocessableEntity, Description = "Failed to parse request")]
    public async Task<IActionResult> EditModuleSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/config/{config_id}/{module_type}/{module_id}/settings")] HttpRequest req,
        FunctionContext context,
        string config_id,
        string module_type,
        string module_id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminModuleSettings))
            return new ForbiddenResult();

        var platform = await _storage.RetrieveAsync<PlatformEntity>(config_id, AireConstants.PlatformConfigRowKey);
        if (platform == null)
            return new NotFoundResult();

        if (platform.Platform == null)
            throw new Exception("Default platform not configured!");

        ModuleType moduleType;
        switch (module_type.ToLower())
        {
            case "id": moduleType = ModuleType.ID; break;
            case "ai": moduleType = ModuleType.AI; break;
            case "memory": moduleType = ModuleType.Memory; break;
            default:
                return new BadRequestResult();
        }

        var config = platform.Platform;
        var modulesByType = config.Modules ?? [];
        if (!modulesByType.TryGetValue(moduleType, out List<Module>? moduleList))
            return new NotFoundResult();

        var module = moduleList.FirstOrDefault(x => x.Id == module_id);
        if (module == null)
            return new NotFoundResult();

        var settings = await req.ReadJson<Dictionary<string, dynamic>>();
        if (settings == null)
            return new UnprocessableEntityResult();

        module.Settings = settings;

        platform.Platform = config;
        bool updated = await _storage.UpsertAsync(platform);
        if (!updated)
            throw new Exception("Failed to update platform instance settings");

        return new OkObjectResult(module.Settings);
    }
}
