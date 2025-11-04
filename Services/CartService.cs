using System.Security.Claims;
using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EBookDashboard.Services
{
    public class CartService : ICartService
    {
        private const decimal TaxRate = 0.08m;

        private readonly IFeatureCartService _featureCartService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IFeatureCartService featureCartService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger)
        {
            _featureCartService = featureCartService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<PaymentSummaryViewModel> GetSummaryAsync()
        {
            if (!TryGetContext(out var sessionId, out var userId))
            {
                return new PaymentSummaryViewModel();
            }

            return await BuildSummaryAsync(sessionId, userId);
        }

        public async Task<CartUpdateResult> AddAsync(int featureId)
        {
            if (!TryGetContext(out var sessionId, out var userId))
            {
                return new CartUpdateResult
                {
                    Message = "Your session has expired. Please sign in again.",
                    ItemCount = 0,
                    Success = false
                };
            }

            try
            {
                var before = await BuildSummaryAsync(sessionId, userId);
                var alreadyInCart = before.CartItems.Any(item => item.FeatureId == featureId);

                var success = await _featureCartService.AddFeatureToTempAsync(sessionId, userId, featureId);

                var after = await BuildSummaryAsync(sessionId, userId);

                return new CartUpdateResult
                {
                    Summary = after,
                    ItemCount = after.CartItems.Count,
                    IsNewItem = !alreadyInCart && success,
                    Message = success
                        ? (alreadyInCart ? "Feature already in your cart." : "Feature added to cart.")
                        : "We couldn't add that feature to your cart.",
                    Success = success
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add feature {FeatureId} to cart", featureId);
                return new CartUpdateResult
                {
                    Message = "Something went wrong while adding the feature. Please try again.",
                    ItemCount = 0,
                    Success = false
                };
            }
        }

        public async Task<CartUpdateResult> RemoveAsync(int featureId)
        {
            if (!TryGetContext(out var sessionId, out var userId))
            {
                return new CartUpdateResult
                {
                    Message = "Your session has expired. Please sign in again.",
                    ItemCount = 0,
                    Success = false
                };
            }

            try
            {
                var success = await _featureCartService.RemoveFeatureFromTempAsync(sessionId, userId, featureId);
                var after = await BuildSummaryAsync(sessionId, userId);

                return new CartUpdateResult
                {
                    Summary = after,
                    ItemCount = after.CartItems.Count,
                    Message = success ? "Feature removed from cart." : "Unable to remove that feature right now.",
                    Success = success
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove feature {FeatureId} from cart", featureId);
                return new CartUpdateResult
                {
                    Message = "Something went wrong while removing the feature. Please try again.",
                    ItemCount = 0,
                    Success = false
                };
            }
        }

        public async Task ClearAsync()
        {
            if (!TryGetContext(out var sessionId, out var userId))
            {
                return;
            }

            try
            {
                await _featureCartService.ClearTempAsync(sessionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cart for user {UserId}", userId);
            }
        }

        private bool TryGetContext(out string sessionId, out string userId)
        {
            sessionId = string.Empty;
            userId = string.Empty;

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return false;
            }

            var session = context.Session;
            if (!session.IsAvailable)
            {
                session.LoadAsync().GetAwaiter().GetResult();
            }

            sessionId = session.Id;
            if (string.IsNullOrEmpty(sessionId))
            {
                // touch the session to ensure an ID is generated
                session.SetString("cart-session-touch", DateTime.UtcNow.ToString("O"));
                sessionId = session.Id;
            }

            userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            return !string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(userId);
        }

        private async Task<PaymentSummaryViewModel> BuildSummaryAsync(string sessionId, string userId)
        {
            var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);

            var items = tempFeatures
                .Select(tf => new CartItemViewModel
                {
                    FeatureId = tf.FeatureId ?? 0,
                    FeatureName = tf.FeatureName ?? tf.PlanFeature?.FeatureName ?? "Feature",
                    Description = tf.Description ?? tf.PlanFeature?.Description ?? string.Empty,
                    FeatureRate = tf.FeatureRate
                })
                .OrderBy(item => item.FeatureName)
                .ToList();

            var subtotal = items.Sum(item => item.FeatureRate);
            var tax = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
            var discount = 0m; // placeholder for future promotions
            var total = subtotal + tax - discount;

            return new PaymentSummaryViewModel
            {
                CartItems = items,
                Subtotal = subtotal,
                Tax = tax,
                Discount = discount,
                Total = total
            };
        }
    }
}


