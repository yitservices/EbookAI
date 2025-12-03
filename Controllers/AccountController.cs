﻿﻿using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;


namespace EBookDashboard.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AccountController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }
        //--------------------------------------------------------------------------
        //-----------------  User Login -------------------------------------
        //--------------------------------------------------------------------------           
         // GET: /Account/Login
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (TempData.ContainsKey("Error"))
            {
                ViewBag.Error = TempData["Error"]?.ToString();
            }

            // Auto-fetch reader details from database
            try
            {
                // Get reader role ID (assuming RoleId = 1 or 2 for Reader)
                var readerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName.ToLower() == "reader");
                
                if (readerRole != null)
                {
                    // Get first reader user from database
                    var reader = await _context.Users
                        .Where(u => u.RoleId == readerRole.RoleId)
                        .FirstOrDefaultAsync();
                    
                    if (reader != null)
                    {
                        // Pass reader details to view for auto-fill
                        ViewBag.ReaderEmail = reader.UserEmail;
                        ViewBag.ReaderPassword = reader.Password;
                        ViewBag.ReaderName = reader.FullName;
                        ViewBag.ReaderId = reader.UserId;
                        ViewBag.ReaderRole = readerRole.RoleName;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break login page
                _logger.LogError(ex, "Error fetching reader details for auto-fill");
            }

            return View();
        }
        
        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string UserEmail, string Password, bool RememberMe)
        {
            int? userId = 0;
            // check against Users table in MySQL
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == UserEmail && u.Password == Password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                userId = user.UserId;
               // ✅ Login success → redirect to main layout (Dashboard, Home, etc.)

                // ✅ fetch role name from Roles table
               var role = await _context.Roles
                    .Where(r => r.RoleId == user.RoleId)
                    .Select(r => r.RoleName)
                    .FirstOrDefaultAsync();

                // ✅ redirect based on role
                // ✅ build claims
                var claims = new List<Claim>
            {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.UserEmail),
            new Claim(ClaimTypes.Role, role ?? "Reader") // default role if null
            };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,   // remember me checkbox
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30) // session timeout
                };

                // ✅ Sign in the user with cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // ✅ Redirect based on role
                if (role != null && role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    // For Author and Reader roles, redirect to AIGenerateBook2
                    return RedirectToAction("AIGenerateBook2", "Books");
                }
            }
            else
            {
                // ❌ Login failed → show error message
                ViewBag.Error = "Invalid username or password. Please try again.";
                ViewBag.UserId = userId; // ✅ send to Razor view
                return View();
            }
        }
        //--------------------------------------------------------------------------
        //-----------------  User Registration -------------------------------------
        //--------------------------------------------------------------------------
        // GET: Registration
        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        // POST: Registration
        [HttpPost]
        public async Task<IActionResult> Register(Users model, string ConfirmPassword)
        {
            
            // Check ConfirmPassword
            if (model.Password != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View("Registration", model);
            }
            // Check if email already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.UserEmail == model.UserEmail);
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered!";
                return View("Registration", model);
            }
            // OK Save the Data

            // Set default values
            model.CreatedAt = DateTime.UtcNow;
            model.LastLoginAt = DateTime.UtcNow;
            model.Status = "Active";
            model.RoleId = 3; // Default Reader Role

            // Save to DB
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            ViewBag.Success = "Account created successfully! Please login.";
            return RedirectToAction("Login", "Account");
        }
        //--------------------------------------------------------------------------
        //-----------------  Forgot Password   --------------------------------------
        //--------------------------------------------------------------------------
        // GET: ForgotPassword Action --> It will return Views\Account\ForgotPassword.cshtml page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
       {
            return View();
        }
        // POST: ForgotPassword Action --> It will generate email and return Views\Account\ForgotPassword.cshtml page
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);

                if (user != null)
                {
                    // Generate a random reset token (you can store it in DB with expiry)
                    var token = Guid.NewGuid().ToString();

                    // Save token in DB (create a PasswordResetTokens table or add field in Users)
                    var resetLink = Url.Action(
                        "ResetPassword",
                        "Account",
                        new { email = model.UserEmail, token = token },
                        Request.Scheme
                    );

                        var emailBody = $@"
                        <h2>Password Reset</h2>
                        <p>Click the link below to reset your password:</p>
                        <p><a href='{resetLink}'>Reset Password</a></p>
                    ";

                    await _emailService.SendEmailAsync(model.UserEmail, "Password Reset Request", emailBody);
                    _logger.LogWarning(resetLink);
                    return RedirectToAction("ForgotPasswordConfirmation", "Account");
                }

                // Don’t reveal if email doesn’t exist
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            return View(model);
        }
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        //--------------------------------------------------------------------------
        //-----------------  Reset Password   --------------------------------------
        //--------------------------------------------------------------------------
        // POST: ForgotPassword Action --> It will generate email and return Views\Account\ForgotPassword.cshtml page
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);
            // if user did not provide valid email address this part will not execut
            if (user == null)
            {
                // don’t reveal user existence
                return View("ResetPasswordConfirmation");
            }

            // ✅ TODO: validate token manually (since no Identity is used)

            // Update password (hash it in production!)
            user.Password = model.Password;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset for {Email}", model.UserEmail);

            return View("ResetPasswordConfirmation");
        }

        // GET: Reset Password Action --> It will Check toke and email and return Message page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError("", "Invalid Password reset token");
                
            }
            return View();
        }
        // Logout
        public async Task<IActionResult> Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            
            // Sign out from cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Login");
        }

        //======================= External Logins (Google / Facebook) =======================
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            // Guard: ensure OAuth credentials exist to avoid invalid_client errors
            bool missing = false;
            if (string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
            {
                var id = _configuration["Authentication:Google:ClientId"];
                var secret = _configuration["Authentication:Google:ClientSecret"];
                missing = string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret) || id.Contains("YOUR_GOOGLE_CLIENT_ID", StringComparison.OrdinalIgnoreCase);
            }
            else if (string.Equals(provider, "Facebook", StringComparison.OrdinalIgnoreCase))
            {
                var id = _configuration["Authentication:Facebook:AppId"];
                var secret = _configuration["Authentication:Facebook:AppSecret"];
                missing = string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret) || id.Contains("YOUR_FACEBOOK_APP_ID", StringComparison.OrdinalIgnoreCase);
            }
            if (missing)
            {
                return RedirectToAction("Login", new { oauthMissing = provider });
            }

            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { provider, returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string provider, string? returnUrl = null, string? remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                TempData["Error"] = $"External login error: {remoteError}";
                return RedirectToAction("Login");
            }

            var authResult = await HttpContext.AuthenticateAsync(provider);
            if (authResult == null || !authResult.Succeeded || authResult.Principal == null)
            {
                TempData["Error"] = "External authentication failed. Please try again.";
                return RedirectToAction("Login");
            }

            var externalPrincipal = authResult.Principal;
            var email = externalPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            var name = externalPrincipal.FindFirst(ClaimTypes.Name)?.Value ?? email ?? "User";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Unable to retrieve email from provider.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email);

            if (user == null)
            {
                // ✅ Auto-provision new user for social login (no SweetAlert)
                user = new Users
                {
                    FullName = name,
                    UserEmail = email,
                    Password = string.Empty, // social accounts don't use local password
                    ConfirmPassword = string.Empty,
                    SecretQuestion = string.Empty,
                    SecretQuestionAnswer = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    RoleId = 3, // default Reader role
                    Status = "Active"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // ✅ Existing user → sign in with cookie and redirect to Dashboard
            HttpContext.Session.SetInt32("UserId", user.UserId);

            var role = await _context.Roles
                .Where(r => r.RoleId == user.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName ?? name),
                new Claim(ClaimTypes.Email, user.UserEmail),
                new Claim(ClaimTypes.Role, role ?? "Reader")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ✅ OAuth users (Google/Facebook) always redirect to AIGenerateBook
            return RedirectToAction("AIGenerateBook2", "Books");
        }

        //public IActionResult Login(string? returnUrl = null, bool sessionExpired = false)
        //{
        //    if (User.Identity?.IsAuthenticated == true)
        //    {
        //        return RedirectToAction("Index", "Dashboard");
        //    }

        //    ViewBag.ReturnUrl = returnUrl;

        //    if (sessionExpired)
        //    {
        //        ViewBag.Message = "Your session has expired. Please log in again.";
        //    }

        //    return View();
        //}

        //// API endpoint for checking authentication status
        //[HttpGet]
        //public IActionResult CheckAuth()
        //{
        //    if (User.Identity?.IsAuthenticated == true)
        //    {
        //        return Ok(new { isAuthenticated = true, userName = User.Identity.Name });
        //    }
        //    return Unauthorized();
        //}

        // API endpoint for checking session status
        //[HttpGet]
        //[Authorize]
        //public IActionResult CheckSession()
        //{
        //    var lastActivity = HttpContext.Session.GetString("LastActivity");
        //    if (lastActivity != null)
        //    {
        //        var lastActivityTime = DateTime.Parse(lastActivity);
        //        var timeRemaining = TimeSpan.FromMinutes(30) - (DateTime.UtcNow - lastActivityTime);

        //        return Ok(new { 
        //            isActive = true, 
        //            timeRemaining = timeRemaining.TotalMinutes,
        //            lastActivity = lastActivityTime
        //        });
        //    }
        //    return Unauthorized();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();
        //        var errorMessage = string.Join(", ", errors);
        //        if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //            return Json(new { success = false, message = errorMessage });
        //        ViewBag.Error = errorMessage;
        //        return View();
        //    }

        //    // Simple authentication - in production, use proper password hashing and database
        //    var users = HttpContext.Session.GetString("RegisteredUsers");
        //    if (!string.IsNullOrEmpty(users))
        //    {
        //        var userList = System.Text.Json.JsonSerializer.Deserialize<List<UserRegistrationModel>>(users);
        //        var user = userList?.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

        //        if (user != null)
        //        {
        //            var claims = new List<Claim>
        //            {
        //                new Claim(ClaimTypes.Name, user.FullName),
        //                new Claim(ClaimTypes.Email, user.Email),
        //                new Claim("AuthorName", user.AuthorName ?? ""),
        //                new Claim("Genre", user.Genre ?? "")
        //            };

        //            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        //            var authProperties = new AuthenticationProperties
        //            {
        //                IsPersistent = model.RememberMe,
        //                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
        //            };

        //            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

        //            if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //                return Json(new { success = true, message = "Login successful!", redirectUrl = "/Dashboard" });
        //            return RedirectToAction("Index", "Dashboard");
        //        }
        //    }

        //    var loginErrorMessage = "Invalid email or password.";
        //    if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //        return Json(new { success = false, message = loginErrorMessage });
        //    ViewBag.Error = loginErrorMessage;
        //    return View();
        //}

        //[HttpPost]  
        //public async Task<IActionResult> Register(UserRegistrationModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();
        //        var errorMessage = string.Join(", ", errors);
        //        if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //            return Json(new { success = false, message = errorMessage });
        //        ViewBag.Error = errorMessage;
        //        return View("Login");
        //    }

        //    if (model.Password != model.ConfirmPassword)
        //    {
        //        var passwordMismatchError = "Passwords do not match!";
        //        if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //            return Json(new { success = false, message = passwordMismatchError });
        //        ViewBag.Error = passwordMismatchError;
        //        return View("Login");
        //    }

        //    // Get existing users from session
        //    var existingUsers = new List<UserRegistrationModel>();
        //    var usersJson = HttpContext.Session.GetString("RegisteredUsers");
        //    if (!string.IsNullOrEmpty(usersJson))
        //    {
        //        existingUsers = System.Text.Json.JsonSerializer.Deserialize<List<UserRegistrationModel>>(usersJson) ?? new List<UserRegistrationModel>();
        //    }

        //    // Check if user already exists
        //    if (existingUsers.Any(u => u.Email == model.Email))
        //    {
        //        var duplicateEmailError = "An account with this email already exists.";
        //        if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //            return Json(new { success = false, message = duplicateEmailError });
        //        ViewBag.Error = duplicateEmailError;
        //        return View("Login");
        //    }

        //    // Add new user
        //    existingUsers.Add(model);

        //    // Save back to session
        //    var updatedUsersJson = System.Text.Json.JsonSerializer.Serialize(existingUsers);
        //    HttpContext.Session.SetString("RegisteredUsers", updatedUsersJson);

        //    if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
        //        return Json(new { success = true, message = "Account created successfully!" });
        //    ViewBag.Success = "Account created successfully! Please login.";
        //    return View("Login");
        //}

        //public async Task<IActionResult> Logout()
        //{
        //    await HttpContext.SignOutAsync("Cookies");
        //    return RedirectToAction("Login");
        //}

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}