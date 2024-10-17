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
    [Function("EditInactivityDuration_v1")]
    [OpenApiOperation(
        operationId: "editInactivityDuration",
        tags: ["Settings"],
        Summary = "Edit inactivity duration",
        Description = "Edit the inactivity duration before a user is automatically logged out.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(InstanceSettings), Description = "Edited instance settings object")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(List<ValidationResult>), Description = "Invalid request with validation errors")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    public async Task<IActionResult> EditInactivityDuration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/settings/inactivity")] HttpRequest req,
        FunctionContext context
    )
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminAccounts))
            return new ForbiddenResult();

        var platformEntity = await _storage.RetrieveAsync<PlatformEntity>(
            AireEnvironment.PlatformConfiguration!,
            AireConstants.PlatformConfigRowKey);

        if (platformEntity is null || platformEntity.Platform is null)
            throw new Exception("Default platform not configured!");

        var settingsEntity = await req.ReadJson<InstanceSettings>();
        if (settingsEntity is null || settingsEntity.InactivityDuration is null)
            return new BadRequestResult();

        var validationContext = new ValidationContext(settingsEntity) { MemberName = nameof(settingsEntity.InactivityDuration) };
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateProperty(settingsEntity.InactivityDuration, validationContext, validationResults);
        if (!isValid)
            return new BadRequestObjectResult(validationResults);

        bool admin = auth.Principal.IsInRole(AireRoles.Admin);
        if (!admin)
            return new ForbiddenResult();

        InstanceSettings updatedSettings = platformEntity.Settings;
        updatedSettings.InactivityDuration = settingsEntity.InactivityDuration;
        platformEntity.Settings = updatedSettings;

        bool updated = await _storage.UpsertAsync(platformEntity);
        if (!updated)
            throw new Exception("Failed to update InactivityDuration setting");

        return new OkObjectResult(platformEntity.Settings);
    }
}
