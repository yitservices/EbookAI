using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using EBookDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using MySqlConnector;
using System.Collections.Generic;
using System.Linq;

namespace EBookDashboard.Controllers
{
    [Authorize]
    [Route("Features")]
    public class FeaturesController : Controller
    {
        private readonly IFeatureCartService _featureCartService;
        private readonly ApplicationDbContext _context;

        public FeaturesController(IFeatureCartService featureCartService, ApplicationDbContext context)
        {
            _featureCartService = featureCartService;
            _context = context;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            // Check if user has an active confirmed plan
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var hasActivePlan = await _featureCartService.HasActivePlanAsync(userId);
            if (hasActivePlan)
            {
                // User already has an active plan, redirect to dashboard
                return RedirectToAction("Index", "Dashboard");
            }

            // Get all features
            var features = await _featureCartService.GetAllFeaturesAsync();

            // Get temp features for this user
            var sessionId = HttpContext.Session.Id;
            var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);

            var viewModel = new FeaturesViewModel
            {
                Features = features.ToList(),
                TempFeatures = tempFeatures.ToList(),
                PlanMap = _featureCartService.GetPlanMap()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route("AddToCart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Validate request
                if (request == null || request.FeatureId <= 0)
                {
                    return Json(new { success = false, message = "Invalid feature ID" });
                }

                // Try to get userId from NameIdentifier, fallback to Email
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Fallbacks: If missing or non-numeric, resolve numeric userId via email
                var hasNumericId = int.TryParse(userId, out _);
                if (string.IsNullOrEmpty(userId) || !hasNumericId)
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == userEmail);
                        if (user != null)
                        {
                            userId = user.UserId.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated. Please login again." });
                }

                // Validate numeric user id before proceeding
                if (!int.TryParse(userId, out var userIdInt))
                {
                    return Json(new { success = false, message = "Cannot resolve numeric user id. Please re-login." });
                }

                // Preflight: verify temp cart table exists and is usable
                try
                {
                    // Will throw if the table is missing or schema mismatched
                    var _ = await _context.AuthorPlanFeaturesSet.Take(1).AnyAsync();
                }
                catch (Exception ex)
                {
                    var hint = "Run update_database.sql on the app's database to create AuthorPlanFeatures.";
                    return Json(new { success = false, message = $"Cart storage not ready: {ex.Message}", hint });
                }

                // If feature already in temp cart for this user, short-circuit with friendly message
                var sessionId = HttpContext.Session.Id;

                var alreadyInCart = await _context.AuthorPlanFeaturesSet
                    .AnyAsync(tf => (tf.UserId == userIdInt || tf.AuthorId == userIdInt) && tf.FeatureId == request.FeatureId && tf.Status == "temp");
                if (alreadyInCart)
                {
                    var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
                    return Json(new
                    {
                        success = true,
                        alreadyInCart = true,
                        message = "Feature already in cart",
                        totalItems = tempFeatures.Count()
                    });
                }

                // Check if feature exists in database and is active
                var feature = await _context.PlanFeatures
                    .FirstOrDefaultAsync(f => f.FeatureId == request.FeatureId);

                if (feature == null)
                {
                    return Json(new { success = false, message = $"Feature with ID {request.FeatureId} not found." });
                }

                if (!(feature.IsActive == true))
                {
                    return Json(new { success = false, message = $"Feature '{feature.FeatureName}' is not active." });
                }

                Console.WriteLine($"AddToCart: Adding feature '{feature.FeatureName}' (ID: {feature.FeatureId}, Rate: {feature.FeatureRate}) for user {userId}");

                var result = await _featureCartService.AddFeatureToTempAsync(sessionId, userId, request.FeatureId);

                if (result)
                {
                    // Get updated temp features count
                    var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
                    var totalCount = tempFeatures.Count();

                    // Get suggested plan
                    var suggestedPlan = await _featureCartService.SuggestPlanAsync(sessionId, userId);

                    return Json(new
                    {
                        success = true,
                        totalItems = totalCount,
                        message = "Feature added to cart successfully",
                        suggestedPlan = suggestedPlan != null ? new
                        {
                            id = suggestedPlan.PlanId,
                            name = suggestedPlan.PlanName,
                            price = suggestedPlan.PlanRate
                        } : null
                    });
                }

                return Json(new { success = false, message = "Failed to add feature to cart. Please try again." });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while adding the feature. Please try again later.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("RemoveFromCart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var sessionId = HttpContext.Session.Id;
            var result = await _featureCartService.RemoveFeatureFromTempAsync(sessionId, userId, request.FeatureId);

            if (result)
            {
                // Get updated temp features count
                var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
                var totalCount = tempFeatures.Count();

                // Get suggested plan
                var suggestedPlan = await _featureCartService.SuggestPlanAsync(sessionId, userId);

                return Json(new
                {
                    success = true,
                    totalItems = totalCount,
                    suggestedPlan = suggestedPlan != null ? new
                    {
                        id = suggestedPlan.PlanId,
                        name = suggestedPlan.PlanName,
                        price = suggestedPlan.PlanRate
                    } : null
                });
            }

            return Json(new { success = false, message = "Failed to remove feature from cart" });
        }

        [HttpPost]
        [Route("ConfirmPlan")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPlan([FromBody] ConfirmPlanRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var sessionId = HttpContext.Session.Id;
            var result = await _featureCartService.ConfirmPlanAndMigrateAsync(sessionId, userId, request.PlanId);

            if (result)
            {
                return Json(new { success = true, redirect = "/Dashboard/Index" });
            }

            return Json(new { success = false, message = "Failed to confirm plan" });
        }

        [HttpGet]
        [Route("CartPreview")]
        public async Task<IActionResult> CartPreview()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var sessionId = HttpContext.Session.Id;
            var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
            var total = await _featureCartService.CalculateTotalTempPriceAsync(sessionId, userId);

            var cartItems = tempFeatures.Select(tf => new EBookDashboard.Models.CartItemViewModel
            {
                FeatureId = tf.FeatureId ?? 0,
                FeatureName = tf.FeatureName ?? "",
                Description = tf.Description ?? "",
                FeatureRate = tf.FeatureRate
            }).ToList();

            return Json(new
            {
                success = true,
                items = cartItems,
                total = total,
                count = cartItems.Count
            });
        }

        [HttpGet]
        [Route("All")]
        public async Task<IActionResult> All()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Fetch active features; if none, seed a sensible default set
            var features = (await _featureCartService.GetAllFeaturesAsync()).ToList();
            if (features.Count == 0)
            {
                var defaults = new List<PlanFeatures>
                {
                    new PlanFeatures { FeatureName = "Bold & Underline", Description = "Emphasize text with bold and underline styling.", FeatureRate = 299, Currency = "USD", IsActive = true },
                    new PlanFeatures { FeatureName = "Custom Fonts", Description = "Use premium fonts to enhance readability.", FeatureRate = 399, Currency = "USD", IsActive = true },
                    new PlanFeatures { FeatureName = "Advanced Formatting", Description = "Professional layout controls and spacing.", FeatureRate = 499, Currency = "USD", IsActive = true },
                    new PlanFeatures { FeatureName = "Export to PDF", Description = "Export your book to high-quality PDF.", FeatureRate = 599, Currency = "USD", IsActive = true },
                    new PlanFeatures { FeatureName = "AI Writer", Description = "Generate and refine content with AI assistance.", FeatureRate = 799, Currency = "USD", IsActive = true }
                };

                _context.PlanFeatures.AddRange(defaults);
                await _context.SaveChangesAsync();
                features = defaults;
            }

            var result = features.Select(f => new
            {
                id = f.FeatureId,
                name = f.FeatureName ?? string.Empty,
                description = f.Description ?? string.Empty,
                price = f.FeatureRate,
                currency = f.Currency ?? "USD"
            }).ToList();

            return Json(new { success = true, features = result });
        }

        [HttpGet]
        [Route("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var feature = await _context.PlanFeatures.FindAsync(id);
            if (feature == null)
            {
                return Json(new { success = false, message = "Feature not found" });
            }

            return Json(new
            {
                success = true,
                feature = new
                {
                    id = feature.FeatureId,
                    name = feature.FeatureName ?? string.Empty,
                    description = feature.Description ?? string.Empty,
                    price = feature.FeatureRate,
                    currency = feature.Currency ?? "USD"
                }
            });
        }

        // GET: Features/GetFeatureDetails
        [HttpGet]
        [Route("GetFeatureDetails/{featureType}")]
        public async Task<IActionResult> GetFeatureDetails(string featureType)
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                Response.StatusCode = 401;
                // For AJAX requests, return JSON; for regular requests, return view
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { error = "Unauthorized", message = "Please login to access features" });
                }
                return Unauthorized();
            }

            // Try to get userId from NameIdentifier, fallback to Email claim
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Fallback: If NameIdentifier is missing, try to get user ID from Email
            if (string.IsNullOrEmpty(userId))
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    // Try to get user ID from database using email
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserEmail == userEmail);
                    if (user != null)
                    {
                        userId = user.UserId.ToString();
                    }
                }
            }

            // If still no userId, return error
            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 401;
                // For AJAX requests, return JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { error = "Unauthorized", message = "Please refresh the page and login again to access features." });
                }
                return Unauthorized();
            }

            // Always ensure features exist for this type first - this will create them if they don't exist
            List<PlanFeatures> features = new List<PlanFeatures>();

            try
            {
                // This method will create features in the database if they don't exist
                var ensuredFeatures = await EnsureFeaturesForTypeAsync(featureType);

                if (ensuredFeatures != null && ensuredFeatures.Any())
                {
                    features = ensuredFeatures;
                    Console.WriteLine($"GetFeatureDetails: Returning {features.Count} features for type '{featureType}' from EnsureFeaturesForTypeAsync");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetFeatureDetails EnsureFeaturesForTypeAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Continue to try getting from database
            }

            // If still no features, try to get ALL active features from database
            if (!features.Any())
            {
                try
                {
                    features = await _context.PlanFeatures
                        .AsNoTracking()
                        .Where(f => f.IsActive == true)
                        .ToListAsync();

                    Console.WriteLine($"GetFeatureDetails: Found {features.Count} active features from database");

                    // Set defaults for any null optional columns
                    foreach (var feature in features)
                    {
                        if (string.IsNullOrEmpty(feature.DeliveryTime))
                            feature.DeliveryTime = "Standard";
                        if (string.IsNullOrEmpty(feature.Revisions))
                            feature.Revisions = "As needed";
                        if (string.IsNullOrEmpty(feature.IconClass))
                            feature.IconClass = GetIconClassForFeature(feature.FeatureName ?? "");
                        if (feature.OriginalPrice == 0 && feature.FeatureRate > 0)
                            feature.OriginalPrice = feature.FeatureRate * 1.5m;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error querying database for features: {ex.Message}");
                }
            }

            // If STILL no features, create default dummy features in the database
            if (!features.Any())
            {
                Console.WriteLine($"WARNING: No features found. Creating default dummy features in database for '{featureType}'");

                try
                {
                    // Create features directly in the database
                    var dummyFeatures = new List<PlanFeatures>
                    {
                        new PlanFeatures
                        {
                            FeatureName = "Bold/Underline Text",
                            Description = "Format text with bold and underline styling options.",
                            FeatureRate = 4.99m,
                            OriginalPrice = 9.99m,
                            Currency = "USD",
                            Status = "premium",
                            IsActive = true
                        },
                        new PlanFeatures
                        {
                            FeatureName = "Custom Fonts",
                            Description = "Access premium font library with hundreds of professional typefaces.",
                            FeatureRate = 9.99m,
                            OriginalPrice = 19.99m,
                            Currency = "USD",
                            Status = "premium",
                            IsActive = true
                        },
                        new PlanFeatures
                        {
                            FeatureName = "Text Formatting Suite",
                            Description = "Complete text formatting: italic, strikethrough, highlight, and more.",
                            FeatureRate = 14.99m,
                            OriginalPrice = 29.99m,
                            Currency = "USD",
                            Status = "premium",
                            IsActive = true
                        },
                        new PlanFeatures
                        {
                            FeatureName = "Export to PDF",
                            Description = "Export your formatted book to professional PDF format.",
                            FeatureRate = 19.99m,
                            OriginalPrice = 39.99m,
                            Currency = "USD",
                            Status = "premium",
                            IsActive = true
                        }
                    };

                    // Save to database one by one
                    foreach (var feature in dummyFeatures)
                    {
                        try
                        {
                            // Check if it already exists
                            var exists = await _context.PlanFeatures
                                .AnyAsync(f => f.FeatureName != null &&
                                               f.FeatureName.Equals(feature.FeatureName, StringComparison.OrdinalIgnoreCase));

                            if (!exists)
                            {
                                _context.PlanFeatures.Add(feature);
                                await _context.SaveChangesAsync();
                                Console.WriteLine($"Created feature in database: {feature.FeatureName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating feature '{feature.FeatureName}': {ex.Message}");
                        }
                    }

                    // Now reload from database
                    features = await _context.PlanFeatures
                        .AsNoTracking()
                        .Where(f => f.IsActive == true)
                        .ToListAsync();

                    // Set optional properties
                    foreach (var feature in features)
                    {
                        if (string.IsNullOrEmpty(feature.DeliveryTime))
                            feature.DeliveryTime = "Instant";
                        if (string.IsNullOrEmpty(feature.Revisions))
                            feature.Revisions = "Unlimited";
                        if (string.IsNullOrEmpty(feature.IconClass))
                            feature.IconClass = GetIconClassForFeature(feature.FeatureName ?? "");
                        if (feature.OriginalPrice == 0 && feature.FeatureRate > 0)
                            feature.OriginalPrice = feature.FeatureRate * 1.5m;
                    }

                    Console.WriteLine($"Loaded {features.Count} features from database after creating dummy features");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating dummy features: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Continue - will return empty list
                }
            }

            // Ensure Content-Type is set correctly for AJAX
            Response.ContentType = "text/html; charset=utf-8";
            return PartialView("FeatureDetails", features);
        }

        private static readonly Dictionary<string, List<PlanFeatureDefinition>> FeatureDefinitionsByType = new()
        {
            ["ai cover design"] = new List<PlanFeatureDefinition>
            {
                new("AI Cover Design – Starter", "Generate stunning cover concepts with guided AI styling.", 49.99m, 69.99m, "fas fa-palette", "3-4 days", "2 revisions"),
                new("AI Cover Design – Professional", "Unlimited AI explorations with art-director review.", 89.99m, 129.99m, "fas fa-brush", "2-3 days", "Unlimited"),
                new("AI Cover Design – Elite", "Custom typography, premium renders, and market-ready exports.", 129.99m, 189.99m, "fas fa-gem", "1-2 days", "Unlimited")
            },
            ["audio book creation"] = new List<PlanFeatureDefinition>
            {
                new("Audio Book Creation – Essentials", "Professional voice talent with studio-grade mastering.", 199.99m, 279.99m, "fas fa-microphone", "7-10 days", "2 revisions"),
                new("Audio Book Creation – Pro Studio", "Multiple narrators, SFX, and platform-ready mastering.", 349.99m, 449.99m, "fas fa-headphones", "5-7 days", "Unlimited"),
                new("Audio Book Creation – Cinematic", "Full-cast narration, cinematic scoring, immersive experience.", 599.99m, 799.99m, "fas fa-music", "10-14 days", "Unlimited")
            },
            ["advanced analytics"] = new List<PlanFeatureDefinition>
            {
                new("Advanced Analytics – Insights", "Real-time sales, heatmaps, and segment insights.", 29.99m, 39.99m, "fas fa-chart-line", "Instant", "N/A"),
                new("Advanced Analytics – Growth", "Forecasting, cohorts, and AI-powered recommendations.", 49.99m, 69.99m, "fas fa-chart-area", "Instant", "N/A"),
                new("Advanced Analytics – Enterprise", "Full market benchmarks, data exports, and API access.", 79.99m, 109.99m, "fas fa-chart-pie", "Instant", "N/A")
            },
            ["priority support"] = new List<PlanFeatureDefinition>
            {
                new("Priority Support – Fast Track", "Fast-lane responses, publishing checklists, and QA.", 19.99m, 29.99m, "fas fa-life-ring", "Instant", "N/A"),
                new("Priority Support – Dedicated", "Dedicated specialist, scheduled reviews, tailored advice.", 39.99m, 59.99m, "fas fa-user-tie", "Instant", "N/A"),
                new("Priority Support – Concierge", "24/7 concierge, launch orchestration, proactive alerts.", 79.99m, 99.99m, "fas fa-hands-helping", "Instant", "N/A")
            },
            ["premium features"] = new List<PlanFeatureDefinition>
            {
                new("Interactive eBook Creator", "Transform your manuscript into immersive, multimedia experiences.", 79.99m, 119.99m, "fas fa-book-open", "5-7 days", "3 revisions"),
                new("Marketing Automation Suite", "Automated campaigns, drip journeys, and smart retargeting.", 89.99m, 139.99m, "fas fa-bullhorn", "3-5 days", "Unlimited"),
                new("Reader Community Platform", "Build and moderate your premium reader community.", 69.99m, 99.99m, "fas fa-users", "5-7 days", "Unlimited")
            },
            ["formatting features"] = new List<PlanFeatureDefinition>
            {
                new("Bold/Underline Text", "Format text with bold and underline styling options.", 4.99m, 9.99m, "fas fa-bold", "Instant", "Unlimited"),
                new("Custom Fonts", "Access premium font library with hundreds of professional typefaces.", 9.99m, 19.99m, "fas fa-font", "Instant", "Unlimited"),
                new("Text Formatting Suite", "Complete text formatting: italic, strikethrough, highlight, and more.", 14.99m, 29.99m, "fas fa-text-height", "Instant", "Unlimited"),
                new("Paragraph Styling", "Advanced paragraph formatting with spacing, alignment, and indentation.", 7.99m, 14.99m, "fas fa-align-left", "Instant", "Unlimited"),
                new("Export to PDF", "Export your formatted book to professional PDF format.", 19.99m, 39.99m, "fas fa-file-pdf", "Instant", "Unlimited"),
                new("Premium Writing Suite", "Complete bundle: Bold/Underline, Custom Fonts, Text Formatting, Paragraph Styling, and PDF Export.", 39.99m, 79.99m, "fas fa-box", "Instant", "Unlimited")
            }
        };

        private async Task<List<PlanFeatures>> EnsureFeaturesForTypeAsync(string featureType)
        {
            var normalizedType = (featureType ?? "premium features").Trim().ToLowerInvariant();

            if (!FeatureDefinitionsByType.TryGetValue(normalizedType, out var definitions))
            {
                definitions = FeatureDefinitionsByType["premium features"];
            }

            var featureNameSet = new HashSet<string>(definitions.Select(d => d.FeatureName), StringComparer.OrdinalIgnoreCase);

            var existingFeatures = await _context.PlanFeatures
                .Where(f => f.FeatureName != null && featureNameSet.Contains(f.FeatureName))
                .ToListAsync();

            var existingLookup = existingFeatures
                .Where(f => !string.IsNullOrEmpty(f.FeatureName))
                .ToDictionary(f => f.FeatureName!, f => f, StringComparer.OrdinalIgnoreCase);
            var featuresToInsert = new List<PlanFeatures>();

            Console.WriteLine($"EnsureFeaturesForTypeAsync: Found {existingFeatures.Count} existing features for type '{featureType}'");

            foreach (var def in definitions)
            {
                if (!existingLookup.TryGetValue(def.FeatureName, out var existing))
                {
                    var newFeature = new PlanFeatures
                    {
                        FeatureName = def.FeatureName,
                        Description = def.Description,
                        FeatureRate = def.FeatureRate,
                        OriginalPrice = def.OriginalPrice, // Set OriginalPrice for new features
                        Currency = def.Currency,
                        Status = "premium",
                        IsActive = true
                    };

                    featuresToInsert.Add(newFeature);
                    Console.WriteLine($"Creating new feature: {def.FeatureName} with rate: {def.FeatureRate}");
                }
                else
                {
                    existing.FeatureRate = def.FeatureRate;
                    existing.OriginalPrice = def.OriginalPrice; // Update OriginalPrice for existing features
                    existing.Description ??= def.Description;
                    existing.Currency ??= def.Currency;
                    existing.Status ??= "premium";
                    existing.IsActive = true;
                    Console.WriteLine($"Updating existing feature: {def.FeatureName} with rate: {def.FeatureRate}");
                }
            }

            if (featuresToInsert.Any())
            {
                try
                {
                    // Validate all features before inserting
                    var validFeatures = new List<PlanFeatures>();

                    foreach (var feature in featuresToInsert)
                    {
                        if (string.IsNullOrWhiteSpace(feature.FeatureName))
                        {
                            Console.WriteLine($"WARNING: Skipping feature with empty name");
                            continue;
                        }

                        // Ensure all required fields have default values
                        feature.Currency ??= "USD";
                        feature.Status ??= "premium";
                        feature.IsActive ??= true;
                        feature.FeatureRate = feature.FeatureRate <= 0 ? 0.01m : feature.FeatureRate;

                        validFeatures.Add(feature);
                    }

                    if (validFeatures.Any())
                    {
                        // Insert one by one to catch any individual errors
                        var successfullyInserted = new List<PlanFeatures>();

                        foreach (var feature in validFeatures)
                        {
                            try
                            {
                                // Check if it already exists (race condition protection)
                                var exists = await _context.PlanFeatures
                                    .AnyAsync(f => f.FeatureName != null &&
                                                   f.FeatureName.Equals(feature.FeatureName, StringComparison.OrdinalIgnoreCase));

                                if (!exists)
                                {
                                    _context.PlanFeatures.Add(feature);
                                    await _context.SaveChangesAsync();
                                    successfullyInserted.Add(feature);
                                    Console.WriteLine($"Successfully inserted feature: {feature.FeatureName}");
                                }
                                else
                                {
                                    Console.WriteLine($"Feature {feature.FeatureName} already exists, skipping insert");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error inserting feature '{feature.FeatureName}': {ex.Message}");
                                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                                // Continue with other features
                            }
                        }

                        // Reload successfully inserted features to get their IDs
                        if (successfullyInserted.Any())
                        {
                            var insertedFeatureNames = successfullyInserted.Select(f => f.FeatureName).ToList();
                            var insertedFeatures = await _context.PlanFeatures
                                .Where(f => f.FeatureName != null && insertedFeatureNames.Contains(f.FeatureName))
                                .AsNoTracking()
                                .ToListAsync();

                            existingFeatures.AddRange(insertedFeatures);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CRITICAL ERROR in EnsureFeaturesForTypeAsync insert: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    // Don't throw - continue with existing features
                }
            }

            // Update existing features in the database (they're already tracked from the query above)
            if (existingFeatures.Any())
            {
                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Updated {existingFeatures.Count} existing features");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating existing features: {ex.Message}");
                    // Continue - features are already in the list
                }
            }

            // Reload all features (existing + newly inserted) to get fresh data with correct IDs
            var refreshedFeatures = await _context.PlanFeatures
                .Where(f => f.FeatureName != null && featureNameSet.Contains(f.FeatureName) && f.IsActive == true)
                .AsNoTracking()
                .ToListAsync();

            Console.WriteLine($"EnsureFeaturesForTypeAsync: Returning {refreshedFeatures.Count} features with IDs: {string.Join(", ", refreshedFeatures.Select(f => f.FeatureId))}");

            foreach (var feature in refreshedFeatures)
            {
                var def = definitions.First(d => d.FeatureName.Equals(feature.FeatureName, StringComparison.OrdinalIgnoreCase));
                feature.DeliveryTime = def.DeliveryTime;
                feature.Revisions = def.Revisions;
                feature.IconClass = def.IconClass;
                feature.OriginalPrice = def.OriginalPrice;
            }

            return refreshedFeatures;
        }

        private record PlanFeatureDefinition(string FeatureName, string Description, decimal FeatureRate, decimal OriginalPrice, string IconClass, string DeliveryTime, string Revisions, string Currency = "USD");

        // Helper class to compare PlanFeatures for distinct
        private class PlanFeaturesComparer : IEqualityComparer<PlanFeatures>
        {
            public bool Equals(PlanFeatures? x, PlanFeatures? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                // Compare FeatureId first (more reliable)
                if (x.FeatureId > 0 && y.FeatureId > 0)
                {
                    return x.FeatureId == y.FeatureId;
                }

                // Fallback to FeatureName comparison
                var xName = x.FeatureName ?? string.Empty;
                var yName = y.FeatureName ?? string.Empty;
                return xName.Equals(yName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(PlanFeatures obj)
            {
                if (obj == null) return 0;

                // Use FeatureId if available
                if (obj.FeatureId > 0)
                {
                    return obj.FeatureId.GetHashCode();
                }

                // Fallback to FeatureName
                return obj.FeatureName?.GetHashCode() ?? 0;
            }
        }

        // Helper method to get icon class based on feature name
        private string GetIconClassForFeature(string featureName)
        {
            featureName = featureName.ToLower();

            if (featureName.Contains("cover") || featureName.Contains("design"))
                return "fas fa-palette";
            if (featureName.Contains("audio") || featureName.Contains("voice"))
                return "fas fa-microphone";
            if (featureName.Contains("analytic") || featureName.Contains("report"))
                return "fas fa-chart-bar";
            if (featureName.Contains("support") || featureName.Contains("help"))
                return "fas fa-headset";
            if (featureName.Contains("market") || featureName.Contains("promo"))
                return "fas fa-bullhorn";

            return "fas fa-star"; // default icon
        }

        [HttpGet]
        [Route("Cart")]
        public async Task<IActionResult> Cart()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var sessionId = HttpContext.Session.Id;
            var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);

            return View(tempFeatures);
        }

        // GET: Features/GetCartSummary
        [HttpGet]
        [Route("GetCartSummary")]
        public async Task<IActionResult> GetCartSummary()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var sessionId = HttpContext.Session.Id;
            var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
            var total = await _featureCartService.CalculateTotalTempPriceAsync(sessionId, userId);

            // Calculate tax (10% for example)
            var tax = total * 0.1m;

            // Calculate discount (0 for now)
            var discount = 0m;

            // Calculate final total
            var finalTotal = total + tax - discount;

            var cartItems = tempFeatures.Select(tf => new CartItemViewModel
            {
                Id = tf.FeatureId ?? 0,
                Name = tf.FeatureName ?? "",
                Description = tf.Description ?? "",
                Price = tf.FeatureRate
            }).ToList();

            return Json(new
            {
                success = true,
                items = cartItems,
                count = cartItems.Count,
                subtotal = total,
                tax = tax,
                discount = discount,
                total = finalTotal
            });
        }

        [HttpPost]
        [Route("AddMultipleToCart")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleToCart([FromBody] AddMultipleToCartModel model)
        {
            try
            {
                if (model?.FeatureIds == null || !model.FeatureIds.Any())
                {
                    return Json(new { success = false, message = "No features selected" });
                }

                // Get user ID (using your existing logic)
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Fallback to email if needed (using your existing logic)
                if (string.IsNullOrEmpty(userId))
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == userEmail);
                        if (user != null)
                        {
                            userId = user.UserId.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Ensure numeric user id
                if (!int.TryParse(userId, out var userIdInt))
                {
                    return Json(new { success = false, message = "Cannot resolve numeric user id. Please re-login." });
                }

                // Preflight: verify cart storage table exists
                try
                {
                    var _ = await _context.AuthorPlanFeaturesSet.Take(1).AnyAsync();
                }
                catch (Exception ex)
                {
                    var hint = "Run update_database.sql on the app's database to create AuthorPlanFeatures.";
                    return Json(new { success = false, message = $"Cart storage not ready: {ex.Message}", hint });
                }

                var sessionId = HttpContext.Session.Id;
                int addedCount = 0;
                int alreadyInCartCount = 0;
                int errorCount = 0;

                // Process each feature
                foreach (var featureId in model.FeatureIds)
                {
                    // Validate feature existence and active state
                    var feature = await _context.PlanFeatures.FirstOrDefaultAsync(f => f.FeatureId == featureId);
                    if (feature == null)
                    {
                        errorCount++;
                        continue;
                    }
                    if (!(feature.IsActive == true))
                    {
                        errorCount++;
                        continue;
                    }

                    // Check if already in temp cart
                    var alreadyInCart = await _context.AuthorPlanFeaturesSet
                        .AnyAsync(tf => (tf.UserId == userIdInt || tf.AuthorId == userIdInt) &&
                                       tf.FeatureId == featureId && tf.Status == "temp");

                    if (alreadyInCart)
                    {
                        alreadyInCartCount++;
                        continue;
                    }

                    // Add to cart using your existing service
                    var result = await _featureCartService.AddFeatureToTempAsync(sessionId, userId, featureId);
                    if (result)
                    {
                        addedCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }

                // Get updated cart info
                var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
                var totalCount = tempFeatures.Count();
                var suggestedPlan = await _featureCartService.SuggestPlanAsync(sessionId, userId);

                if (addedCount > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"{addedCount} feature(s) added to cart successfully",
                        totalItems = totalCount,
                        addedCount = addedCount,
                        alreadyInCartCount = alreadyInCartCount,
                        errorCount = errorCount,
                        suggestedPlan = suggestedPlan != null ? new
                        {
                            id = suggestedPlan.PlanId,
                            name = suggestedPlan.PlanName,
                            price = suggestedPlan.PlanRate
                        } : null
                    });
                }
                else if (alreadyInCartCount > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "All selected features were already in your cart",
                        totalItems = totalCount,
                        addedCount = 0,
                        alreadyInCartCount = alreadyInCartCount,
                        errorCount = errorCount,
                        alreadyInCart = true,
                        suggestedPlan = suggestedPlan != null ? new
                        {
                            id = suggestedPlan.PlanId,
                            name = suggestedPlan.PlanName,
                            price = suggestedPlan.PlanRate
                        } : null
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = errorCount > 0 ? "Unable to add selected features (validation or DB error)." : "Failed to add features to cart",
                        errorCount = errorCount
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddMultipleToCart: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while adding features to cart"
                });
            }
        }

        // Add this model class inside your FeaturesController class (at the bottom with other models)
        public class AddMultipleToCartModel
        {
            public List<int> FeatureIds { get; set; } = new List<int>();
        }

    }

    // View Models
    public class FeaturesViewModel
    {
        public List<PlanFeatures> Features { get; set; } = new List<PlanFeatures>();
        public List<AuthorPlanFeatures> TempFeatures { get; set; } = new List<AuthorPlanFeatures>();
        public Dictionary<string, int> PlanMap { get; set; } = new Dictionary<string, int>();
    }



    public class CartItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
    }

    // Request Models
    public class AddToCartRequest
    {
        public int FeatureId { get; set; }
    }

    public class RemoveFromCartRequest
    {
        public int FeatureId { get; set; }
    }

    public class ConfirmPlanRequest
    {
        public int PlanId { get; set; }
    }
}
