// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
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
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.UseNewtonsoftJson();
        worker.UseJwtAuth(new JwtTokenServiceConfiguration()
        {
            SigningKey = AireEnvironment.TokenSigningKey,
            EncryptionKey = AireEnvironment.TokenEncryptionKey
        });
    })
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddApplicationInsightsTelemetryWorkerService();

        services.AddAzureClients(builder =>
        {
            builder.AddTableServiceClient(AireEnvironment.StorageConnectionString)
                .ConfigureOptions(options =>
                {
                    options.Diagnostics.IsLoggingEnabled = false;
                });
        });
        
        services.AddSingleton<ITableStorageService, TableStorageService>();

        services.AddMvcCore().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        });

        services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
        {
            var options = new OpenApiConfigurationOptions
            {
                Info = new OpenApiInfo
                {
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