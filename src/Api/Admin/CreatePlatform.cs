using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Aire.Sdk.Azure;
using Aire.Services.Models;
using Aire.Sdk.Models.Platform;
using Aire.Sdk.AspNetCore;

namespace Aire.Services.Api.Admin;

public class CreateDefaultPlatform
{
    private readonly ILogger<CreateDefaultPlatform> _log;
    private readonly ITableStorageService _storage;

    public CreateDefaultPlatform(ILogger<CreateDefaultPlatform> log, ITableStorageService storage)
    {
        _log = log;
        _storage = storage;
    }

    [Function("CreatePlatform")]
    [OpenApiIgnore]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "platform/{platform_name}")] HttpRequest req,
        string platform_name)
    {
        var pk = platform_name;
        var rk = AireConstants.PlatformConfigRowKey;
        var platform = await req.ReadJson<Platform>();

        if (platform == null)
            return new BadRequestResult();

        var ent = new PlatformEntity
        {
            PartitionKey = pk,
            RowKey = rk,
            Platform = platform
        };

        var result = await _storage.UpsertAsync(ent);
        if (!result)
            throw new Exception("Failed to insert entity");

        return new NoContentResult();
    }
}
