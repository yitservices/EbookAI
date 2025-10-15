using EBookDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Stripe.Checkout;
using Stripe.Forwarding;
using System;
using System.Text.RegularExpressions;


namespace EBookDashboard.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Confirm Payment
        [HttpPost]
        public IActionResult ProcessPayment(int authorPlanId)
        {
            var plan = _context.AuthorPlans.Find(authorPlanId);
            if (plan == null) return NotFound();

            // TODO: Replace with real Stripe/PayPal payment logic
            plan.PaymentReference = Guid.NewGuid().ToString();
            plan.IsActive = true;

            _context.SaveChanges();

            return RedirectToAction("Index", "Dashboard");
        }

        // Start Checkout
        public async Task<IActionResult> Checkout(int planId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = _context.Users.FirstOrDefault(u => u.FullName == username);
            if (user == null)
            {
                return Unauthorized();
            }

            int userId = user.UserId;
            int authorId = user.UserId;
            // Fetch plan from DB instead of helper method
            var planDetails = await _context.Plans.FirstOrDefaultAsync(p => p.PlanId == planId);
            //var planDetails = await _context.Plans.FirstOrDefault(p => p.PlanId == planId);
            if (planDetails == null)
            {
                return NotFound();
            }

            // Get logged-in user info safely
            //var userIdStr = User.FindFirst("UserId")?.Value;
            //var authorIdStr = User.FindFirst("AuthorId")?.Value;

            //if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(authorIdStr))
            //{
            //    //return Unauthorized();
            //}

            //int userId = int.Parse(userIdStr);
            //int authorId = int.Parse(authorIdStr);

            // Create AuthorPlan record
            var authorPlan = new AuthorPlans
            {
                AuthorId = authorId,
                UserId = userId,
                PlanId = planDetails.PlanId,
                PlanName = planDetails.PlanName,
                PlanDescription = planDetails.PlanDescription,
                PlanRate = planDetails.PlanRate,
                PlanDays = planDetails.PlanDays,
                PlanHours = 0,
                MaxEBooks = planDetails.MaxEBooks,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(planDetails.PlanDays),
                IsActive = false,
                TrialUsed = false,
                PaymentReference = null
            };

            _context.AuthorPlans.Add(authorPlan);
            _context.SaveChanges();

            // Send to checkout view
            return View("Checkout", authorPlan);
        }
        // Show all active plan features as cards
        public async Task<IActionResult> Index()
        {
            // This is the normal page view (if you visit /Payments/Index directly)
            // Fetch all features from your PlanFeatures table
            var features = await _context.PlanFeatures.ToListAsync();

            // Pass the data to the Razor view
            return View(features);
        }

        [HttpGet]
        public async Task<IActionResult> LoadFeatureLocksAsync()
        {
            // Return your Razor partial view (or the same Index view if you prefer)
            var features = await _context.PlanFeatures.ToListAsync();
            return PartialView("_FeatureLocks", features); // ✅ loads the new partial view
        }



        //=================== xxx =================
        // GET: /Payment/Plans
        //public IActionResult Plans()
        //{
        //    var plans = _context.Plans.ToList(); // Fetch from database
        //    return View(plans); // Send to view
        //}

        //// GET: /Payments/Payment
        //[HttpGet]
        //public IActionResult Payment()
        //{
        //    // Fetch all available plans
        //    var plans = _context.Plans.ToList();

        //    // Pass to view
        //    return View(plans);
        //}
        //// Confirm Payment it is just saving Guid in AuthorPlans table
        //[HttpPost]
        //public IActionResult ProcessPayment1(int authorPlanId)
        //{
        //    var plan = _context.AuthorPlans.Find(authorPlanId);
        //    if (plan == null) return NotFound();

        //    // TODO: Replace with real Stripe/PayPal payment logic
        //    plan.PaymentReference = Guid.NewGuid().ToString();
        //    plan.IsActive = true;

        //    _context.SaveChanges();

        //    return RedirectToAction("Index", "Dashboard");
        //}

        //// Start Checkout : Just saving AuthorPlans only
        //public IActionResult Checkout1(string planName)
        //{
        //    // Fetch plan from DB instead of helper method
        //    var planDetails = _context.Plans.FirstOrDefault(p => p.PlanName == planName);
        //    if (planDetails == null)
        //    {
        //        return NotFound();
        //    }

        //    // Get logged-in user info safely
        //    var userIdStr = User.FindFirst("UserId")?.Value;
        //    var authorIdStr = User.FindFirst("AuthorId")?.Value;

        //    if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(authorIdStr))
        //    {
        //        return Unauthorized();
        //    }

        //    int userId = int.Parse(userIdStr);
        //    int authorId = int.Parse(authorIdStr);
        //    // Create AuthorPlan record
        //    var authorPlan = new AuthorPlans
        //    {
        //        AuthorId = authorId,
        //        UserId = userId,
        //        PlanId = planDetails.PlanId,
        //        PlanDescription = planDetails.PlanDescription,
        //        PlanRate = planDetails.PlanRate,
        //        PlanDays = planDetails.PlanDays,
        //        PlanHours = 0,
        //        MaxEBooks = planDetails.MaxEBooks,
        //        StartDate = DateTime.UtcNow,
        //        EndDate = DateTime.UtcNow.AddDays(planDetails.PlanDays),
        //        IsActive = false, // not active until paid
        //        TrialUsed = false,
        //        PaymentReference = null
        //    };

        //    _context.AuthorPlans.Add(authorPlan);
        //    _context.SaveChanges();

        //    // Send to checkout view
        //    return View("Checkout", authorPlan);
        //}

        //// Stripe Checkout Session Creation
        //// POST: api/checkout/create-session/{userId}
        //[HttpPost("create-session/{userId}")]
        //public async Task<IActionResult> CreateCheckoutSession(int userId)
        //{
        //    // Step 1: Get the active plan for this author
        //    var activePlan = await _context.AuthorPlans
        //        .Where(p => p.UserId == userId && p.IsActive == true)
        //        .FirstOrDefaultAsync();

        //    if (activePlan == null)
        //        return BadRequest("No active plan found for this user.");

        //    // Step 2: Ensure required fields are available
        //    if (string.IsNullOrWhiteSpace(activePlan.UserEmail))
        //        return BadRequest("Customer email is missing for this plan.");

        //    if (activePlan.PlanRate <= 0)
        //        return BadRequest("Invalid plan rate.");

        //    // Step 3: Build domain URLs
        //    var domain = $"{Request.Scheme}://{Request.Host}/";

        //    // Step 4: Configure Stripe session options
        //    var options = new SessionCreateOptions
        //    {
        //        SuccessUrl = domain + "api/checkout/order-confirmation?session_id={CHECKOUT_SESSION_ID}",
        //        CancelUrl = domain + "api/checkout/cancel",
        //        Mode = "payment",
        //        CustomerEmail = activePlan.UserEmail,
        //        LineItems = new List<SessionLineItemOptions>
        //{
        //    new SessionLineItemOptions
        //    {
        //        PriceData = new SessionLineItemPriceDataOptions
        //        {
        //            UnitAmount = (long)(activePlan.PlanRate * 100), // Stripe expects price in cents
        //            Currency = activePlan.Currency ?? "USD",
        //            ProductData = new SessionLineItemPriceDataProductDataOptions
        //            {
        //             //   Name = activePlan.PlanName ?? "Subscription Plan",
        //             //   Description = $"{activePlan.PlanFeature1 ?? ""} {activePlan.PlanFeature2 ?? ""}".Trim()
        //            }
        //        },
        //        //Quantity = activePlan.Quantity ?? 1
        //        Quantity = 1
        //    }
        //}
        //    };

        //    // Step 5: Create the Stripe session
        //    var service = new SessionService();
        //    var session = await service.CreateAsync(options);

        //    // Step 6: Return the Stripe Checkout URL
        //    return Ok(new { sessionUrl = session.Url });
        //}
        //// ✅ 1️⃣ Show all active plan features as cards
        ////public async Task<IActionResult> Index()
        ////{
        ////    //var features = await _context.PlansFeatures
        ////    //    .Include(f => f.plan.PlansFeatures)
        ////    //    .Where(f => f.isActive == 1)
        ////    //    .ToListAsync();

        ////    return View();
        ////}

        //// ✅ 2️⃣ Create Stripe session for selected feature cards
        //[HttpPost("Payments/CreateSession")]
        //public async Task<IActionResult> CreateSession([FromBody] List<int> featureIds)
        //{
        //    //if (featureIds == null || !featureIds.Any())
        //    //    return BadRequest("No items selected.");

        //    //var selectedFeatures = await _context.PlansFeatures
        //    //    .Where(f => featureIds.Contains(f.FeatureId))
        //    //    .ToListAsync();

        //    //if (!selectedFeatures.Any())
        //    //    return BadRequest("Selected features not found.");

        //    //var domain = $"{Request.Scheme}://{Request.Host}/";
        //    //var options = new SessionCreateOptions
        //    //{
        //    //    SuccessUrl = domain + "Payments/Success",
        //    //    CancelUrl = domain + "Payments/Cancel",
        //    //    LineItems = new List<SessionLineItemOptions>(),
        //    //    Mode = "payment"
        //    //};

        //    //foreach (var feature in selectedFeatures)
        //    //{
        //    //    options.LineItems.Add(new SessionLineItemOptions
        //    //    {
        //    //        PriceData = new SessionLineItemPriceDataOptions
        //    //        {
        //    //            UnitAmount = (long)(feature.PlanRate * 100),
        //    //            Currency = feature.Currency ?? "usd",
        //    //            ProductData = new SessionLineItemPriceDataProductDataOptions
        //    //            {
        //    //                Name = feature.FeatureName
        //    //            }
        //    //        },
        //    //        Quantity = 1
        //    //    });
        //    //}

        //    //var service = new SessionService();
        //    //var session = await service.CreateAsync(options);

        //    //// ✅ Optional: Save Bill
        //    //var bill = new Bill
        //    //{
        //    //    Features = string.Join(", ", selectedFeatures.Select(f => f.FeatureName)),
        //    //    TotalAmount = selectedFeatures.Sum(f => f.PlanRate),
        //    //    Currency = selectedFeatures.First().Currency
        //    //};
        //    //_context.Bill.Add(bill);
        //    //await _context.SaveChangesAsync();

        //    return Ok(new { });
        //}

        //public IActionResult Success() => View();
        //public IActionResult Cancel() => View();

    }

}