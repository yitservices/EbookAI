using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Services
{
    public class FeatureCartService : IFeatureCartService
    {
        private readonly ApplicationDbContext _context;

        public FeatureCartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PlanFeatures>> GetAllFeaturesAsync()
        {
            return await _context.PlanFeatures
                .Where(f => f.IsActive == true)
                .OrderBy(f => f.FeatureName)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuthorPlanFeatures>> GetTempFeaturesAsync(string sessionId, string userId)
        {
            // First try to find by UserId, then by AuthorId if not found
            int userIdInt = 0;
            int.TryParse(userId, out userIdInt);
            
            // Don't use Include to avoid join table issues - we already have FeatureId
            return await _context.AuthorPlanFeaturesSet
                .Where(tf => tf.UserId == userIdInt || tf.AuthorId == userIdInt)
                .ToListAsync();
        }

        public async Task<bool> AddFeatureToTempAsync(string sessionId, string userId, int featureId)
        {
            try
            {
                if (featureId <= 0)
                {
                    Console.WriteLine($"Invalid featureId: {featureId}");
                    return false;
                }

                int userIdInt = 0;
                if (!int.TryParse(userId, out userIdInt) || userIdInt <= 0)
                {
                    Console.WriteLine($"Invalid userId: {userId}");
                    return false;
                }
                
                // Check if feature already exists in temp cart for this user
                var existingTempFeature = await _context.AuthorPlanFeaturesSet
                    .Where(tf => (tf.UserId == userIdInt || tf.AuthorId == userIdInt) && tf.FeatureId == featureId && tf.Status == "temp")
                    .FirstOrDefaultAsync();

                if (existingTempFeature != null)
                {
                    // Feature already in cart, don't add duplicate
                    Console.WriteLine($"Feature {featureId} already in cart for user {userIdInt}");
                    return true;
                }

                // Get the feature details
                var feature = await _context.PlanFeatures
                    .Where(f => f.FeatureId == featureId && f.IsActive == true)
                    .FirstOrDefaultAsync();
                    
                if (feature == null)
                {
                    Console.WriteLine($"Feature {featureId} not found or is inactive");
                    return false;
                }

                // Add feature to temp cart
                var tempFeature = new AuthorPlanFeatures
                {
                    UserId = userIdInt,
                    AuthorId = userIdInt,
                    FeatureId = featureId,
                    FeatureName = feature.FeatureName ?? "Unknown Feature",
                    Description = feature.Description,
                    FeatureRate = feature.FeatureRate,
                    Currency = feature.Currency ?? "USD",
                    Status = "temp",
                    IsActive = 1,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuthorPlanFeaturesSet.Add(tempFeature);
                var saveResult = await _context.SaveChangesAsync();

                Console.WriteLine($"Feature {featureId} added to cart successfully. Save result: {saveResult}");
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error adding feature to cart: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> RemoveFeatureFromTempAsync(string sessionId, string userId, int featureId)
        {
            try
            {
                int userIdInt = 0;
                int.TryParse(userId, out userIdInt);
                
                var tempFeature = await _context.AuthorPlanFeaturesSet
                    .Where(tf => (tf.UserId == userIdInt || tf.AuthorId == userIdInt) && tf.FeatureId == featureId)
                    .FirstOrDefaultAsync();

                if (tempFeature != null)
                {
                    _context.AuthorPlanFeaturesSet.Remove(tempFeature);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error removing feature from cart: {ex.Message}");
                return false;
            }
        }

        public async Task<decimal> CalculateTotalTempPriceAsync(string sessionId, string userId)
        {
            int userIdInt = 0;
            int.TryParse(userId, out userIdInt);
            
            var tempFeatures = await _context.AuthorPlanFeaturesSet
                .Where(tf => tf.UserId == userIdInt || tf.AuthorId == userIdInt)
                .ToListAsync();

            return tempFeatures.Sum(tf => tf.FeatureRate);
        }

        public async Task<Plans?> SuggestPlanAsync(string sessionId, string userId)
        {
            int userIdInt = 0;
            int.TryParse(userId, out userIdInt);
            
            var tempFeatures = await GetTempFeaturesAsync(sessionId, userId);
            var featureCount = tempFeatures.Count();

            // Get all plans
            var plans = await _context.Plans.OrderBy(p => p.PlanRate).ToListAsync();

            // Suggest plan based on number of features
            if (featureCount >= 7)
            {
                // Premium plan (most expensive)
                return plans.LastOrDefault();
            }
            else if (featureCount >= 4)
            {
                // Pro plan (middle)
                return plans.Skip(1).FirstOrDefault();
            }
            else if (featureCount >= 1)
            {
                // Basic plan (cheapest, but not free trial)
                return plans.Skip(1).FirstOrDefault() ?? plans.FirstOrDefault(p => p.PlanId != 1);
            }

            return null;
        }

        public async Task<bool> ConfirmPlanAndMigrateAsync(string sessionId, string userId, int planId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int userIdInt = 0;
                int.TryParse(userId, out userIdInt);
                
                // Get the plan
                var plan = await _context.Plans.FindAsync(planId);
                if (plan == null)
                {
                    return false;
                }

                // Create author plan
                var authorPlan = new AuthorPlans
                {
                    AuthorId = userIdInt,
                    PlanId = planId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(plan.PlanDays),
                    IsActive = true,
                    TrialUsed = true, // Mark trial as used since they're selecting a paid plan
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuthorPlans.Add(authorPlan);
                await _context.SaveChangesAsync();

                // Get temp features
                var tempFeatures = await _context.AuthorPlanFeaturesSet
                    .Where(tf => tf.UserId == userIdInt || tf.AuthorId == userIdInt)
                    .ToListAsync();

                // Update temp features with the confirmed plan ID
                foreach (var tempFeature in tempFeatures)
                {
                    tempFeature.PlanId = planId;
                    tempFeature.Status = "confirmed";
                    tempFeature.IsActive = 1;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error confirming plan: {ex.Message}");
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> ClearTempAsync(string sessionId, string userId)
        {
            try
            {
                int userIdInt = 0;
                int.TryParse(userId, out userIdInt);
                
                // Only remove temp features (status = "temp"), not confirmed ones
                var tempFeatures = await _context.AuthorPlanFeaturesSet
                    .Where(tf => (tf.UserId == userIdInt || tf.AuthorId == userIdInt) && tf.Status == "temp")
                    .ToListAsync();

                if (tempFeatures.Any())
                {
                    _context.AuthorPlanFeaturesSet.RemoveRange(tempFeatures);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Cleared {tempFeatures.Count} temp features for user {userIdInt}");
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error clearing temp features: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> HasActivePlanAsync(string userId)
        {
            // For now, let's simplify this to just check if the user has any active plans
            // This is a temporary fix to allow login to work
            int userIdInt = 0;
            int.TryParse(userId, out userIdInt);
            
            var activePlan = await _context.AuthorPlans
                .Where(ap => ap.AuthorId == userIdInt && ap.IsActive && ap.EndDate > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            return activePlan != null;
        }

        public async Task<IEnumerable<AuthorPlanFeatures>> GetUserFeaturesAsync(string userId)
        {
            int userIdInt = 0;
            int.TryParse(userId, out userIdInt);
            
            // Don't use Include to avoid join table issues - we already have FeatureId
            return await _context.AuthorPlanFeaturesSet
                .Where(uf => (uf.UserId == userIdInt || uf.AuthorId == userIdInt) && uf.Status == "confirmed")
                .ToListAsync();
        }

        public Dictionary<string, int> GetPlanMap()
        {
            // Return a dictionary mapping plan names to IDs
            return new Dictionary<string, int>
            {
                { "Basic Plan", 2 },
                { "Pro Plan", 3 },
                { "Premium Plan", 4 }
            };
        }


    }
}