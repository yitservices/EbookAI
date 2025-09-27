using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using EBookDashboard.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Add global authorization filter
    options.Filters.Add(new AuthorizeFilter());
});

// Add enhanced session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "EBookDashboard.Session";
});

// Add enhanced authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "EBookDashboard.Auth";
        
        // Custom events
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    // Add custom policies
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireClaim("Role", "Admin"));
});

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:5282")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add data protection
builder.Services.AddDataProtection();

// Add anti-forgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    
    // Add security headers in production
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}
else
{
    app.UseDeveloperExceptionPage();
}

// Enable response compression
app.UseResponseCompression();

// Enable CORS
app.UseCors();

// HTTPS redirection
app.UseHttpsRedirection();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
        }
    }
});

// Routing
app.UseRouting();

// Session middleware (before authentication)
app.UseSession();

// Custom session management middleware
app.UseSessionManagement();

// Authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Configure routing
app.MapStaticAssets();

// Define routes with constraints
app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}",
    defaults: new { controller = "Account" },
    constraints: new { action = "Login|Register|Logout|AccessDenied" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "Healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName 
}));

// Add API endpoints for AJAX calls
app.MapGet("/Account/CheckSession", async (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new { isAuthenticated = true });
    }
    return Results.Unauthorized();
});

app.MapGet("/Account/CheckAuth", async (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new { isAuthenticated = true });
    }
    return Results.Unauthorized();
});


app.Run();
