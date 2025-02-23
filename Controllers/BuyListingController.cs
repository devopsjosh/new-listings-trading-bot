using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using new_listing_bot_cs.Services;

namespace new_listing_bot_cs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuyListingController : ControllerBase
    {
        private readonly ILogger<BuyListingController> _logger;
        private readonly OrderService _orderService;

        public BuyListingController(ILogger<BuyListingController> logger, OrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpPost]
        [Route("push-symbol")]
        public async Task<IActionResult> BuyListing([FromBody] string symbol)
        {
            var (success, message) = await _orderService.BuyListingAsync(symbol);
            if (success)
            {
                return Ok(message);
            }
            return BadRequest(message);
        }
    }
}