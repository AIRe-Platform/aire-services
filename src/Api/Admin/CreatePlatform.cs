using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Aire.Sdk.Azure;
using Aire.Sdk.Helpers;
using Aire.Services.Models;
using Aire.Services;

namespace Aire.Servces.Api.Admin
{
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
            PlatformConfiguration? config = null;
            var pk = platform_name;
            var rk = AireConstants.PlatformConfigRowKey;

            try
            {
                using var stream = new StreamReader(req.Body);
                var configData = await stream.ReadToEndAsync();
                if(configData.Length > 0)
                {
                    config = configData.JsonToObject<PlatformConfiguration>();
                }
            }
            catch(Exception ex)
            {
                _log.LogError(ex, "Failed to read request body");
                return new BadRequestResult();
            }
            
            if(config == null)
                return new BadRequestResult();

            var ent = new PlatformEntity 
            {
                PartitionKey = pk,
                RowKey = rk,
                Config = config
            };

            var result = await _storage.UpsertAsync(ent);

            if(result)
                return new NoContentResult();
            else
                return new InternalServerErrorResult();
        }
    }
}
