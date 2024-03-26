using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Aire.Sdk.Azure;
using Aire.Services;
using Aire.Sdk.Auth;
using Aire.Sdk.Auth.Extensions;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker => {
        worker.UseNewtonsoftJson();        
        worker.UseJwtAuth(new JwtTokenServiceConfiguration() {
            SigningKey = AireEnvironment.TokenSigningKey,
            EncryptionKey = AireEnvironment.TokenEncryptionKey
        });
    })
    .ConfigureServices(services => {
        services.AddHttpClient();
        services.AddApplicationInsightsTelemetryWorkerService();

        services
            .AddSingleton<ITableStorageService, TableStorageService>()
            .Configure<TableStorageConfiguration>(o => {
                o.ConnectionString = AireEnvironment.StorageConnectionString;
            });

        services.AddMvcCore().AddNewtonsoftJson(options => {
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });

        services.AddSingleton<IOpenApiConfigurationOptions>(_ => {
            var options = new OpenApiConfigurationOptions {
                Info = new OpenApiInfo {
                    Version = "0.1.0",
                    Title = "AIRe Services Module",
                    Description = "This is the reference implementation of the AIRe Platform Services module."
                },
                Servers = [
                    new OpenApiServer { Url = AireEnvironment.OpenApiHost ?? "/api" }
                ],
                OpenApiVersion = OpenApiVersionType.V3,
                IncludeRequestingHostName = false,
                ForceHttp = false,
                ForceHttps = false,
            };

            return options;
        });

        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();