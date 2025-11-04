using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using EBookDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add services to the container.
builder.Services.AddControllersWithViews();
// Session is configured later with options, don't add it twice

// ✅ Add CORS services (for API calls, if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost5282",
        policy =>
        {
            // Allow all localhost ports for development
            policy.SetIsOriginAllowed(origin => 
                origin.StartsWith("http://localhost") || 
                origin.StartsWith("https://localhost") ||
                origin.StartsWith("http://127.0.0.1") ||
                origin.StartsWith("https://127.0.0.1"))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Allow credentials for cookie-based auth
        });
});

// ✅ Configure MySQL DbContext
// Enable retry on failure for database connections
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 34)), // adjust version to your MySQL
        mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    ));

// ✅ Enable Authentication with Cookie Scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // where to go if not logged in
        options.LogoutPath = "/Account/Logout";      // logout URL
        options.AccessDeniedPath = "/Account/AccessDenied"; // unauthorized
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);  // session timeout
        options.SlidingExpiration = true;            // extend session if active
        options.Cookie.SameSite = SameSiteMode.Lax;  // Allow cookies for same-site requests
        options.Cookie.HttpOnly = true;              // Prevent XSS attacks
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Use HTTPS in production
        
        // Handle AJAX requests - return 401 instead of redirect
        options.Events.OnRedirectToLogin = context =>
        {
            var contentType = context.Request.Headers["Content-Type"].ToString();
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                !string.IsNullOrEmpty(contentType) && contentType.Contains("application/json"))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Unauthorized\",\"message\":\"Please login to continue\"}");
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var contentType = context.Request.Headers["Content-Type"].ToString();
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                !string.IsNullOrEmpty(contentType) && contentType.Contains("application/json"))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Forbidden\",\"message\":\"Access denied\"}");
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ✅ Authorization middleware (roles, policies etc.)
builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserService, UserService>();
// Add EmailService
builder.Services.AddScoped<IEmailService, EmailService>();
// Add BookService
builder.Services.AddScoped<IBookService, BookService>();
// Add PlanService
builder.Services.AddScoped<IPlanService, PlanService>();
// Add FeatureCartService
builder.Services.AddScoped<IFeatureCartService, FeatureCartService>();
// Add CheckoutService
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
// Register Plan Features Service
builder.Services.AddScoped<IPlanFeaturesService, PlanFeaturesService>();
//
builder.Services.AddScoped<IAPIRawResponseService, APIRawResponseService>();
builder.Services.AddScoped<ICartService, CartService>();
// Add this to your services
builder.Services.AddScoped<BookProcessingService>();
// Add FeatureAccessService for paid feature checks
builder.Services.AddScoped<FeatureAccessService>();

// Add session services
builder.Services.AddDistributedMemoryCache();
//Register HttpClient factory
builder.Services.AddHttpClient();
//Register Sessions
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ✅ Middleware pipeline
// Enable detailed error pages in development for better debugging
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // ✅ enable session middleware - MUST be before authentication

// ✅ Enable CORS - MUST be after UseRouting but before UseEndpoints
app.UseCors("AllowLocalhost5282");

// ✅ Authentication + Authorization - MUST be in this order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Role-based dashboard routes
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{action=Dashboard}/{id?}",
    defaults: new { controller = "Admin" });

app.MapControllerRoute(
    name: "author",
    pattern: "Author/{action=Dashboard}/{id?}",
    defaults: new { controller = "Author" });

app.MapControllerRoute(
    name: "reader",
    pattern: "Reader/{action=Dashboard}/{id?}",
    defaults: new { controller = "Reader" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "features",
    pattern: "Features/{action=Index}/{id?}",
    defaults: new { controller = "Features" });

app.MapControllerRoute(
    name: "payments",
    pattern: "Payments/{action=Index}/{id?}",
    defaults: new { controller = "Payments" });

app.MapControllerRoute(
    name: "checkout",
    pattern: "Checkout/{action}/{id?}",
    defaults: new { controller = "Checkout", action = "GetPublishableKey" });

app.Run();