using EBookDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Controllers
{
    public class PlansController : Controller
    {
        private readonly IPlanService _planService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        public PlansController(IPlanService planService, IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _planService = planService;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var plans = await _planService.GetActivePlansAsync();
            return View(plans);
        }

        [HttpPost]
        public async Task<IActionResult> BuyPlan(int planId)
        {
            var plan = (await _planService.GetActivePlansAsync()).FirstOrDefault(p => p.PlanId == planId);
            if (plan == null) return NotFound();

            // 🔗 Call WebPaymentAPI
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/"); // your WebPaymentAPI base URL
            // call method of WebPaymentAPI-->[HttpPost("create-session-form")]
            var response = await client.PostAsJsonAsync("api/checkout/create-session-form", new
            {
                itemName = plan.PlanName,
                itemPrice = plan.PlanRate,
                quantity = 1,
                currency = "USD",
                customerEmail = User.Identity?.Name ?? "test@example.com"
            });

            if (!response.IsSuccessStatusCode)
                return BadRequest("Unable to create payment session");

            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            string sessionUrl = result?.sessionUrl;

            return Redirect(sessionUrl);
        }
        //-------- added on 09-10-2025
        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _context.Plans
                    .Where(p => p.IsActive == true) // Only get active plans
                    .Select(p => new
                    {
                        p.PlanId,
                        p.PlanName,
                        p.PlanRate,
                        p.Currency,
                        p.PlanDays,
                        p.MaxEBooks,
                        p.PlanDescription
                    })
                    .ToListAsync();

                // If no plans found, return empty array
                if (!plans.Any())
                {
                    return Json(new List<object>());
                }

                return Json(plans);
            }
            catch (Exception ex)
            {
                // Log error (in production, use proper logging)
                Console.WriteLine($"Error fetching plans: {ex.Message}");
                
                // Return empty array on error to prevent frontend crash
                return Json(new List<object>());
            }
        }

    }

}
