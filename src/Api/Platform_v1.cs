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

public class Platform_v1(IJwtTokenService _jwt, ITableStorageService _storage)
{
    [Function("ListPlatforms_v1")]
    [OpenApiOperation("listPlatforms", ["Configuration"], Summary = "List available platforms")]
    [OpenApiSecurity("AireServiceKey", SecuritySchemeType.ApiKey,
        Name = "Aire-Service-Key",
        In = OpenApiSecurityLocationType.Header,
        Description = "Internal platform module service key")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http,
        Scheme = OpenApiSecuritySchemeType.Bearer,
        BearerFormat = "JWT",
        Description = "User token")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Dictionary<string, PlatformInfo>), Description = "Platform by ID")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Login required")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    public async Task<IActionResult> ListPlatforms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/configs/platforms")] HttpRequest req,
        FunctionContext context)
    {
        var results = new Dictionary<string, PlatformInfo>();
        var entities = await _storage.All<PlatformEntity>();

        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
        {
            if (!req.IsServiceRequest())
                return new UnauthorizedResult();
        }
        else if (!_jwt.CheckAuthorization(auth, AireScopes.ListPlatforms))
            return new ForbiddenResult();

        foreach (var entity in entities)
        {
            if (entity?.Platform?.Name == null)
                continue;

            var info = new PlatformInfo
            {
                Name = entity.Platform.Name
            };

            results.Add(entity.PartitionKey!, info);
        }

        return new OkObjectResult(results);
    }
}
