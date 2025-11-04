using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace EBookDashboard.Controllers
{
    [Route("[controller]/[action]")]
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly IFeatureCartService _featureCartService;

        public CheckoutController(ICheckoutService checkoutService, IConfiguration config, ApplicationDbContext context, IFeatureCartService featureCartService)
        {
            _checkoutService = checkoutService;
            _config = config;
            _context = context;
            _featureCartService = featureCartService;
        }

        // FIXED: Changed route to match what JavaScript is calling
        [HttpGet]
        public IActionResult GetPublishableKey()
        {
            var key = _config["Stripe:PublishableKey"];
            if (string.IsNullOrEmpty(key))
                return BadRequest(new { message = "Stripe publishable key not configured." });

            return Ok(new { publishableKey = key });
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CheckoutRequest request)
        {
            try
            {
                var secretKey = _config["Stripe:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                    return BadRequest(new { message = "Stripe secret key not configured." });

                StripeConfiguration.ApiKey = secretKey;

                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Invalid amount." });
                }

                var domain = $"{Request.Scheme}://{Request.Host.Value}";

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = request.Amount,
                                Currency = request.Currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.ProductName,
                                    Description = request.ProductDescription
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = $"{domain}/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{domain}/Checkout/Cancel",
                    ClientReferenceId = request.AuthorPlanId?.ToString() // Track which plan is being purchased
                };

                var service = new SessionService();
                var session = service.Create(options);

                return Ok(new { sessionId = session.Id });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Server error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            // If this is a feature cart payment, clear the cart
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var sessionId = HttpContext.Session.Id;
                await _featureCartService.ClearTempAsync(sessionId, userId);
            }
            
            ViewBag.SessionId = session_id;
            return View();
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }
    }

    public class CheckoutRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public int? AuthorPlanId { get; set; }
    }
}