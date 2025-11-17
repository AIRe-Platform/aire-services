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
    [OpenApiParameter("config_id", Description = "Configuration identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(InstanceSettings), Description = "Edited instance settings object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Configuration not found")]
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
}
