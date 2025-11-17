// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using System.Net;
using Aire.Sdk.AspNetCore;
using Aire.Sdk.Auth;
using Aire.Sdk.Auth.Extensions;
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

public class AgentConfig_v1(IJwtTokenService _jwt, ITableStorageService _storage)
{
    [Function("GetAgentConfiguration_v1")]
    [OpenApiOperation("getAgentConfiguration", ["Agents"], Summary = "Get agent configurations")]
    [OpenApiSecurity("AireServiceKey", SecuritySchemeType.ApiKey,
        Name = "Aire-Service-Key",
        In = OpenApiSecurityLocationType.Header,
        Description = "Internal platform module service key")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("config_id", Description = "Configuration identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<AgentConfig>), Description = "List of agents")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Module settings not found")]
    public async Task<IActionResult> GetAgentConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/config/{config_id}/agents")] HttpRequest req,
            FunctionContext context,
            string config_id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
        {
            if (!req.IsServiceRequest())
                return new UnauthorizedResult();
        }
        else if (!_jwt.CheckAuthorization(auth, AireScopes.AdminAgents))
            return new ForbiddenResult();

        var platform = await _storage.RetrieveAsync<PlatformEntity>(config_id, AireConstants.PlatformConfigRowKey);
        if (platform == null)
            return new NotFoundResult();

        return new OkObjectResult(platform.Agents);
    }

    [Function("EditAgentConfiguration_v1")]
    [OpenApiOperation("editAgentConfigutation", ["Agents"], Summary = "Edit agent configuration")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT", Description = "User token")]
    [OpenApiParameter("config_id", Description = "Configuration identifier", Required = true, In = ParameterLocation.Path)]
    [OpenApiRequestBody("application/json", typeof(List<AgentConfig>), Description = "List of agents")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(List<AgentConfig>), Description = "Updated agent configs")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Unauthorized, Description = "Missing or invalid authorization")]
    [OpenApiResponseWithoutBody(HttpStatusCode.Forbidden, Description = "Access denied")]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest, Description = "Invalid request")]
    [OpenApiResponseWithoutBody(HttpStatusCode.UnprocessableEntity, Description = "Could not parse configuration data")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound, Description = "Config not found")]
    public async Task<IActionResult> EditAgentConfiguration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/config/{config_id}/agents")] HttpRequest req,
        FunctionContext context,
        string config_id)
    {
        var auth = context.Features.Get<JwtAuthFeature>();
        if (auth == null)
            return new UnauthorizedResult();

        if (!_jwt.CheckAuthorization(auth, AireScopes.AdminAgents))
            return new ForbiddenResult();

        var platform = await _storage.RetrieveAsync<PlatformEntity>(config_id, AireConstants.PlatformConfigRowKey);
        if (platform == null)
            return new NotFoundResult();

        var agents = await req.ReadJson<List<AgentConfig>>();
        if (agents == null)
            return new UnprocessableEntityResult();

        platform.Agents = agents;

        bool updated = await _storage.UpsertAsync(platform);
        if (!updated)
            throw new Exception("Failed to update module settings");

        return new OkObjectResult(platform.Agents);
    }
}
