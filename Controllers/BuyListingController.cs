using Microsoft.AspNetCore.Mvc;

namespace new_listing_bot_cs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuyListingController : ControllerBase
{
    private readonly ILogger<BuyListingController> _logger;

    private readonly IServiceProvider _serviceProvider;


    public BuyListingController(ILogger<BuyListingController> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [HttpPost]
    [Route("push-symbol")]
    public async Task<IActionResult> BuyListing([FromBody] string symbol)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var (success, message) = await orderService.BuyListingAsync(symbol);
        if (success)
        {
            return Ok(message);
        }
        return BadRequest(message);
    }
}
