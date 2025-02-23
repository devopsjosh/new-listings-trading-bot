
namespace new_listing_bot_cs;

public class BuyListingWorker : BackgroundService
{
    private readonly BotConfig _botConfig;
    private readonly ILogger<BuyListingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly OrderService _orderService;

    public BuyListingWorker(ILogger<BuyListingWorker> logger, IServiceProvider serviceProvider, BotConfig botConfig, OrderService orderService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botConfig = botConfig;
        _orderService = orderService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Listings Worker");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exchangeService = scope.ServiceProvider.GetRequiredService<Exchange>();
                var listingService = scope.ServiceProvider.GetRequiredService<ListingsGetter>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var listings = await listingService.GetListings();
                var latestAnnouncement = listings?.Data?.Catalogs?.FirstOrDefault()?.Articles?.FirstOrDefault()?.Title;

                // latestAnnouncement =
                //     "Binance will list Bitcoin (BTC), Solana (SOL), Ethereum (ETH) and Dogecoin (DOGE)"; // For testing
                // _logger.LogInformation($"Latest Announcement: {latestAnnouncement}");

                if (latestAnnouncement == null || !latestAnnouncement.ToLower().Contains("will list"))
                {
                    // Any lower you'll get rate limited.
                    await Task.Delay(40, stoppingToken);
                    continue;
                }

                _logger.LogInformation($"NEW LISTING ANNOUNCEMENT: {latestAnnouncement}");

                var symbols = ListingsGetter.ExtractSymbols(latestAnnouncement);
                foreach (var symbol in symbols)
                {
                    var (success, message) = await _orderService.BuyListingAsync(symbol);
                    if (!success)
                    {
                        _logger.LogDebug(message);
                    }
                    else
                    {
                        _logger.LogInformation(message);
                    }
                }

                // Sleep for a second - only for testing purposes
                await Task.Delay(300, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in BuyListingWorker, likely rate limited. Sleeping for a minute: {ex}");
                await Task.Delay(60000, stoppingToken);
            }
    }
}