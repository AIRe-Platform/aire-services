using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Aire.Helpers;
using Aire.Services.Models;
using System.Web.Http;
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

        [FunctionName("CreateDefaultPlatform")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req)
        {
            PlatformConfiguration config = null;
            var pk = AireEnvironment.PlatformConfiguration;
            var rk = AireConstants.PlatformConfigRowKey;

            if(req.Body != null)
            {
                try
                {
                    using var stream = new StreamReader(req.Body);
                    var configData = await stream.ReadToEndAsync();
                    config = configData.JsonToObject<PlatformConfiguration>();
                }
                catch(Exception ex)
                {
                    _log.LogError(ex, "Failed to read request body");
                }
            }

            if(config == null)
            {

                var serviceQuery = await _storage.QueryAsync<ServiceEntity>(x => x.PartitionKey == pk);
                var services = new List<Service>();
                await foreach(ServiceEntity svc in serviceQuery)
                {
                    try
                    {
                        services.Add(svc.Service);
                    }
                    catch(Exception ex)
                    {
                        _log.LogError(ex, $"Invalid service entry {svc.RowKey}");
                    }
                }

                config = new PlatformConfiguration
                {
                    Platform = new Platform {
                        Name = "AIRe Development Platform",
                        Modules = new Dictionary<ModuleType, Module> {
                            { 
                                ModuleType.ID, 
                                new Module {
                                    Access = ModuleAccess.Public,
                                    Endpoint = AireEnvironment.IDModuleEndpoint,
                                    Type = ModuleType.ID
                                }
                            },
                            {
                                ModuleType.Memory,
                                new Module {
                                    Access = ModuleAccess.Private,
                                    Endpoint = AireEnvironment.MemoryModuleEndpoint,
                                    Type = ModuleType.Memory
                                }
                            },
                            {
                                ModuleType.AI,
                                new Module {
                                    Access = ModuleAccess.Public,
                                    Endpoint = AireEnvironment.MemoryModuleEndpoint,
                                    Type = ModuleType.Memory
                                }
                            }
                        }
                    },
                    Services = services
                };
            }

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
