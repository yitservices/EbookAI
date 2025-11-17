using EBookDashboard.Models;
using EBookDashboard.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Stripe.Checkout;
using Stripe;
using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace EBookDashboard.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFeatureCartService _featureCartService;
        private readonly ICartService _cartService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public PaymentsController(ApplicationDbContext context, IFeatureCartService featureCartService, ICartService cartService, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _featureCartService = featureCartService;
            _cartService = cartService;
            _configuration = configuration;
            _emailService = emailService;
        }
        
        // Payment Summary page
        [HttpGet]
        public async Task<IActionResult> PaymentSummary()
        {
            var summary = await _cartService.GetSummaryAsync();
            return View(summary);
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

        // Checkout for selected features (cart total)
        [HttpGet]
        public async Task<IActionResult> CheckoutCart()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Payments/CheckoutCart" });
            }

            var sessionId = HttpContext.Session.Id;
            var total = await _featureCartService.CalculateTotalTempPriceAsync(sessionId, userIdClaim);

            // Prefer email claim; fallback to userId
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
            var user = !string.IsNullOrEmpty(email)
                ? await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email)
                : await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToString() == userIdClaim);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Payments/CheckoutCart" });
            }

            var model = new AuthorPlans
            {
                AuthorId = user.UserId,
                UserId = user.UserId,
                PlanId = 0,
                PlanName = "Selected Features",
                PlanDescription = "Features Cart",
                PlanRate = total,
                PlanDays = 0,
                PlanHours = 0,
                MaxEBooks = 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                IsActive = false,
                TrialUsed = false,
                PaymentReference = null
            };

            return View("Checkout", model);
        }

        // Create Stripe checkout session for feature cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                // Fallback: If NameIdentifier is missing, try to get user ID from Email
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserEmail == userEmail);
                        if (user != null)
                        {
                            userIdClaim = user.UserId.ToString();
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new { success = false, message = "User not authenticated. Please login again." });
                }

                var sessionId = HttpContext.Session.Id;
                var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userIdClaim);
                
                if (!tempFeatures.Any())
                {
                    return Json(new { success = false, message = "Your cart is empty. Please add features to your cart first." });
                }

                var lineItems = new List<SessionLineItemOptions>();
                
                foreach (var feature in tempFeatures)
                {
                    if (feature.FeatureRate <= 0)
                    {
                        continue; // Skip features with invalid pricing
                    }
                    
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(feature.FeatureRate * 100), // Convert to cents
                            Currency = feature.Currency?.ToLower() ?? "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = feature.FeatureName ?? "Premium Feature",
                                Description = feature.Description ?? "Professional eBook Publishing Feature"
                            }
                        },
                        Quantity = 1
                    });
                }
                
                if (lineItems.Count == 0)
                {
                    return Json(new { success = false, message = "No valid features found in cart. Please add features with valid pricing." });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = $"{baseUrl}/Payments/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{baseUrl}/Payments/Cancel",
                    ClientReferenceId = userIdClaim,
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userIdClaim },
                        { "sessionId", sessionId },
                        { "featureCount", tempFeatures.Count().ToString() }
                    }
                };

                var secretKey = _configuration.GetSection("Stripe")["SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    return Json(new { success = false, message = "Stripe is not configured. Please contact support." });
                }

                var service = new SessionService();
                StripeConfiguration.ApiKey = secretKey;
                
                var session = await service.CreateAsync(options);
                
                Console.WriteLine($"Stripe checkout session created: {session.Id} for user {userIdClaim}");
                
                return Json(new { success = true, url = session.Url, sessionId = session.Id });
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe error: {e.Message}");
                return Json(new { success = false, message = $"Stripe error: {e.StripeError?.Message ?? e.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating checkout session: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while creating the checkout session. Please try again." });
            }
        }

        // Payment success page
        public async Task<IActionResult> Success(string session_id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                // Fallback: If NameIdentifier is missing, try to get user ID from Email
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.UserEmail == userEmail);
                        if (user != null)
                        {
                            userIdClaim = user.UserId.ToString();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(session_id) && !string.IsNullOrEmpty(userIdClaim))
                {
                    // Verify the session with Stripe
                    var secretKey = _configuration.GetSection("Stripe")["SecretKey"];
                    if (!string.IsNullOrEmpty(secretKey))
                    {
                        StripeConfiguration.ApiKey = secretKey;
                        var service = new SessionService();
                        
                        try
                        {
                            var session = await service.GetAsync(session_id);
                            
                            if (session.PaymentStatus == "paid" && session.ClientReferenceId == userIdClaim)
                            {
                                // Update temp features to confirmed status
                                var sessionId = HttpContext.Session.Id;
                                var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userIdClaim);
                                
                                if (tempFeatures.Any())
                                {
                                    // Get user details for invoice
                                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToString() == userIdClaim);
                                    var userEmail = user?.UserEmail ?? "";
                                    decimal totalAmount = tempFeatures.Sum(f => f.FeatureRate);
                                    
                                    // Update features to Paid status
                                    foreach (var feature in tempFeatures)
                                    {
                                        feature.Status = "Paid"; // Set to "Paid" as required
                                        feature.PaymentReference = session_id;
                                        feature.IsActive = 1;
                                    }
                                    
                                    await _context.SaveChangesAsync();
                                    
                                    // Generate and send PDF invoice
                                    try
                                    {
                                        var invoicePdf = GenerateInvoicePdf(tempFeatures, user, session, totalAmount);
                                        await SendInvoiceEmail(userEmail, user?.FullName ?? "Customer", invoicePdf, session_id);
                                        Console.WriteLine($"Invoice sent to {userEmail}");
                                    }
                                    catch (Exception invoiceEx)
                                    {
                                        Console.WriteLine($"Error generating/sending invoice: {invoiceEx.Message}");
                                    }
                                    
                                    // Clear temp features after confirming
                                    await _featureCartService.ClearTempAsync(sessionId, userIdClaim);
                                    
                                    Console.WriteLine($"Payment verified and features confirmed for user {userIdClaim}");
                                }
                            }
                        }
                        catch (StripeException ex)
                        {
                            Console.WriteLine($"Stripe error verifying session: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Success page: {ex.Message}");
            }

            return View();
        }

        // Payment cancel page
        public IActionResult Cancel()
        {
            return View();
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

        // Webhook to handle Stripe events
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            try
            {
                var webhookSecret = _configuration.GetSection("Stripe")["WebhookSecret"];
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    Console.WriteLine("Stripe webhook secret not configured");
                    return BadRequest();
                }

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    webhookSecret
                );

                // Handle the checkout.session.completed event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    
                    if (session != null && !string.IsNullOrEmpty(session.ClientReferenceId))
                    {
                        var userId = session.ClientReferenceId;
                        
                        Console.WriteLine($"Webhook: Payment completed for user {userId}, session {session.Id}");
                        
                        // Get session ID from metadata if available, otherwise use current session
                        var sessionId = session.Metadata?.ContainsKey("sessionId") == true 
                            ? session.Metadata["sessionId"] 
                            : HttpContext.Session.Id;
                        
                        // Get the user's temp features
                        var tempFeatures = await _featureCartService.GetTempFeaturesAsync(sessionId, userId);
                        
                        if (tempFeatures.Any())
                        {
                            // Get user details for invoice
                            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToString() == userId);
                            var userEmail = user?.UserEmail ?? "";
                            
                            // Calculate total
                            decimal totalAmount = tempFeatures.Sum(f => f.FeatureRate);
                            
                            // Move temp features to confirmed/paid status
                            foreach (var feature in tempFeatures)
                            {
                                feature.Status = "Paid"; // Set to "Paid" as required
                                feature.PaymentReference = session.Id; // Store Stripe session ID
                                feature.IsActive = 1;
                                
                                Console.WriteLine($"Feature {feature.FeatureId} ({feature.FeatureName}) confirmed and paid for user {userId}");
                            }
                            
                            await _context.SaveChangesAsync();
                            
                            // Generate and send PDF invoice
                            try
                            {
                                var invoicePdf = GenerateInvoicePdf(tempFeatures, user, session, totalAmount);
                                await SendInvoiceEmail(userEmail, user?.FullName ?? "Customer", invoicePdf, session.Id);
                                Console.WriteLine($"Invoice sent to {userEmail}");
                            }
                            catch (Exception invoiceEx)
                            {
                                Console.WriteLine($"Error generating/sending invoice: {invoiceEx.Message}");
                                // Don't fail the payment if invoice fails
                            }
                            
                            // Clear temp features after confirming (optional - you might want to keep them)
                            // await _featureCartService.ClearTempAsync(sessionId, userId);
                            
                            Console.WriteLine($"All features confirmed and paid for user {userId}");
                        }
                        else
                        {
                            Console.WriteLine($"No temp features found for user {userId}");
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Stripe webhook error: {ex.Message}");
                return BadRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest();
            }
        }

        // Generate PDF Invoice
        private byte[] GenerateInvoicePdf(IEnumerable<AuthorPlanFeatures> features, Users? user, Session session, decimal totalAmount)
        {
            // Simple HTML-based invoice (in production, use a proper PDF library like iTextSharp, QuestPDF, or DinkToPdf)
            var invoiceHtml = new StringBuilder();
            invoiceHtml.AppendLine("<!DOCTYPE html><html><head><style>");
            invoiceHtml.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
            invoiceHtml.AppendLine("h1 { color: #333; }");
            invoiceHtml.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            invoiceHtml.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
            invoiceHtml.AppendLine("th { background-color: #4a6cf7; color: white; }");
            invoiceHtml.AppendLine(".total { font-weight: bold; font-size: 1.2em; }");
            invoiceHtml.AppendLine("</style></head><body>");
            invoiceHtml.AppendLine($"<h1>Invoice</h1>");
            invoiceHtml.AppendLine($"<p><strong>Invoice Number:</strong> INV-{session.Id.Substring(0, 8).ToUpper()}</p>");
            invoiceHtml.AppendLine($"<p><strong>Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            invoiceHtml.AppendLine($"<p><strong>Payment Reference:</strong> {session.Id}</p>");
            
            if (user != null)
            {
                invoiceHtml.AppendLine($"<h2>Bill To:</h2>");
                invoiceHtml.AppendLine($"<p><strong>{user.FullName}</strong><br>");
                invoiceHtml.AppendLine($"{user.UserEmail}</p>");
            }
            
            invoiceHtml.AppendLine("<h2>Items Purchased:</h2>");
            invoiceHtml.AppendLine("<table>");
            invoiceHtml.AppendLine("<tr><th>Feature Name</th><th>Description</th><th>Price</th></tr>");
            
            foreach (var feature in features)
            {
                invoiceHtml.AppendLine($"<tr>");
                invoiceHtml.AppendLine($"<td>{feature.FeatureName ?? "Unknown Feature"}</td>");
                invoiceHtml.AppendLine($"<td>{feature.Description ?? "Premium Feature"}</td>");
                invoiceHtml.AppendLine($"<td>${feature.FeatureRate:F2} {feature.Currency ?? "USD"}</td>");
                invoiceHtml.AppendLine($"</tr>");
            }
            
            invoiceHtml.AppendLine("</table>");
            invoiceHtml.AppendLine($"<p class='total'>Total Amount: ${totalAmount:F2} USD</p>");
            invoiceHtml.AppendLine("<p><em>Thank you for your purchase!</em></p>");
            invoiceHtml.AppendLine("</body></html>");
            
            // Convert HTML to bytes (in production, use proper PDF generation library)
            return Encoding.UTF8.GetBytes(invoiceHtml.ToString());
        }

        // Send invoice email with PDF attachment
        private async Task SendInvoiceEmail(string toEmail, string customerName, byte[] invoicePdf, string paymentReference)
        {
            if (string.IsNullOrEmpty(toEmail))
            {
                Console.WriteLine("Cannot send invoice: email address is empty");
                return;
            }

            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                using var client = new SmtpClient(smtpServer, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail ?? "noreply@ebookpublisher.com", senderName ?? "eBook Publisher"),
                    Subject = $"Invoice for Your Purchase - {paymentReference.Substring(0, 8).ToUpper()}",
                    Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2>Thank You for Your Purchase!</h2>
    <p>Dear {customerName},</p>
    <p>Your payment has been processed successfully. Please find your invoice attached.</p>
    <p><strong>Payment Reference:</strong> {paymentReference}</p>
    <p>Your purchased features are now active and ready to use.</p>
    <br>
    <p>Best regards,<br>eBook Publisher Team</p>
</body>
</html>",
                    IsBodyHtml = true
                };
                
                mail.To.Add(toEmail);
                
                // Attach invoice PDF
                var stream = new MemoryStream(invoicePdf);
                var attachment = new Attachment(stream, $"Invoice-{paymentReference.Substring(0, 8)}.html", "text/html");
                mail.Attachments.Add(attachment);
                
                await client.SendMailAsync(mail);
                Console.WriteLine($"Invoice email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending invoice email: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - log and continue
            }
        }
    }
}