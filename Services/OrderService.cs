using ExchangeSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace new_listing_bot_cs.Services
{
    public class OrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly BotConfig _botConfig;

        public OrderService(ILogger<OrderService> logger, IServiceProvider serviceProvider, BotConfig botConfig)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _botConfig = botConfig;
        }

        public async Task<(bool Success, string Message)> BuyListingAsync(string symbol)
        {
            using var scope = _serviceProvider.CreateScope();
            var exchangeService = scope.ServiceProvider.GetRequiredService<Exchange>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                var symbolExists = await dbContext.Portfolio
                    .AnyAsync(x => x.ExchangeOrderResult.MarketSymbol == $"{symbol.ToUpper()}_USDT");

                if (symbolExists)
                {
                    _logger.LogDebug($"{symbol} already bought, skipping...");
                    return (false, $"{symbol} already bought, skipping...");
                }

                _logger.LogInformation($"Buying {_botConfig.BuyAmount} of {symbol}. We will only Buy the same asset once.");

                var orderRequest = new ExchangeOrderRequest
                {
                    MarketSymbol = $"{symbol.ToUpper()}_USDT",
                    Amount = _botConfig.BuyAmount,
                    IsBuy = true,
                    OrderType = OrderType.Market,
                    ExtraParameters =
                    {
                        { "amount", _botConfig.BuyAmount } // ExchangeSharp BS. We need to pass amount like this if using Poloniex.
                    }
                };

                var result = await exchangeService.HandlePlaceOrder(orderRequest);
                if (result == null)
                {
                    _logger.LogError($"Failed to place order for {symbol}");
                    return (false, $"Failed to place order for {symbol}");
                }

                var order = new OrderResult
                {
                    ExchangeOrderResult = result,
                    Exit = new Exit
                    {
                        TakeProfitPrice = result.Price + result.Price * _botConfig.TakeProfit / 100,
                        StopLossPrice = result.Price - result.Price * _botConfig.StopLoss / 100
                    }
                };

                dbContext.OrderResults.Add(order);
                dbContext.Portfolio.Add(order);

                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"{symbol} Outcome: {result}");
                return (true, $"{symbol} Outcome: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {symbol}: {ex}");
                return (false, $"Error processing {symbol}: {ex}");
            }
        }
    }
}