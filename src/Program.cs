using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Aire.Services;
using Aire.Helpers;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker => worker.UseNewtonsoftJson())
    .ConfigureOpenApi()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<ITableStorageService, TableStorageService>();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();