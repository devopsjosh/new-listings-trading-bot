using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using new_listing_bot_cs;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Register Config Stuff
        var connectionString = hostContext.Configuration.GetConnectionString("Database");
        var apiKey = hostContext.Configuration.GetSection("ApiConfig:ApiKey").Value;
        var apiSecret = hostContext.Configuration.GetSection("ApiConfig:ApiSecret").Value;
        var exchangeNameStr = hostContext.Configuration.GetSection("ApiConfig:ExchangeName").Value;

        if (string.IsNullOrEmpty(apiSecret)  || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(exchangeNameStr))
        {
            throw new ArgumentNullException(nameof(apiSecret), "ApiSecret, ApiKey, and ExchangeName cannot be null or empty");
        }

        if (!Enum.TryParse(exchangeNameStr, out ExchangeNameEnum exchangeName))
        {
            throw new ArgumentException($"Invalid ExchangeName: {exchangeNameStr}");
        }

        var botConfig = hostContext.Configuration.GetSection("BotConfig").Get<BotConfig>();
        if (botConfig == null)
        {
            throw new ArgumentNullException(nameof(botConfig), "BotConfig cannot be null");
        }

        services.AddSingleton(botConfig);

        // Register Services
        services.AddScoped<Exchange>(provider => new Exchange(exchangeName, apiKey, apiSecret));
        services.AddScoped<ListingsGetter>(provider => new ListingsGetter());

        // Register Db
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Register Workers
        services.AddHostedService<ExitStrategyWorker>();
        services.AddHostedService<BuyListingWorker>();

        // Register Controllers
        services.AddControllers();
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .Build();

// Apply migrations before running the host
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // This will apply pending migrations
}

await host.RunAsync();