using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeatherStationImages.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Register WeatherService with its interface
        services.AddSingleton<IWeatherService, WeatherService>();

        // Register ImageService with its interface
        services.AddSingleton<IImageService>(sp =>
        {
            var connectionString = context.Configuration["AzureWebJobsStorage"]
                ?? "UseDevelopmentStorage=true";
            return new ImageService(connectionString);
        });

        // Register HttpClient
        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();