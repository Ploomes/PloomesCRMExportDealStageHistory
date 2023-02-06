﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PloomesCRMExportDealStageHistory;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.ConfigureServices(services =>
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();

        services.AddHostedService<Exporter>();
        _ = services.AddHttpClient<RequestService>();
        _ = services.AddTransient<PloomesService>();
    });

hostBuilder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
});

IHost host = hostBuilder.Build();

await host.RunAsync();