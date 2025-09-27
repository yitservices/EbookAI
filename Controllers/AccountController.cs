using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EBookDashboard.Models;

namespace EBookDashboard.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        public IActionResult Login(string? returnUrl = null, bool sessionExpired = false)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            
            ViewBag.ReturnUrl = returnUrl;
            
            if (sessionExpired)
            {
                ViewBag.Message = "Your session has expired. Please log in again.";
            }
            
            return View();
        }

        // API endpoint for checking authentication status
        [HttpGet]
        public IActionResult CheckAuth()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new { isAuthenticated = true, userName = User.Identity.Name });
            }
            return Unauthorized();
        }

        // API endpoint for checking session status
        [HttpGet]
        [Authorize]
        public IActionResult CheckSession()
        {
            var lastActivity = HttpContext.Session.GetString("LastActivity");
            if (lastActivity != null)
            {
                var lastActivityTime = DateTime.Parse(lastActivity);
                var timeRemaining = TimeSpan.FromMinutes(30) - (DateTime.UtcNow - lastActivityTime);
                
                return Ok(new { 
                    isActive = true, 
                    timeRemaining = timeRemaining.TotalMinutes,
                    lastActivity = lastActivityTime
                });
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();
                var errorMessage = string.Join(", ", errors);
                if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                    return Json(new { success = false, message = errorMessage });
                ViewBag.Error = errorMessage;
                return View();
            }

            // Simple authentication - in production, use proper password hashing and database
            var users = HttpContext.Session.GetString("RegisteredUsers");
            if (!string.IsNullOrEmpty(users))
            {
                var userList = System.Text.Json.JsonSerializer.Deserialize<List<UserRegistrationModel>>(users);
                var user = userList?.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("AuthorName", user.AuthorName ?? ""),
                        new Claim("Genre", user.Genre ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
                    };

                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);
                    
                    if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                        return Json(new { success = true, message = "Login successful!", redirectUrl = "/Dashboard" });
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            var loginErrorMessage = "Invalid email or password.";
            if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                return Json(new { success = false, message = loginErrorMessage });
            ViewBag.Error = loginErrorMessage;
            return View();
        }

        [HttpPost]  
        public async Task<IActionResult> Register(UserRegistrationModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();
                var errorMessage = string.Join(", ", errors);
                if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                    return Json(new { success = false, message = errorMessage });
                ViewBag.Error = errorMessage;
                return View("Login");
            }

            if (model.Password != model.ConfirmPassword)
            {
                var passwordMismatchError = "Passwords do not match!";
                if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                    return Json(new { success = false, message = passwordMismatchError });
                ViewBag.Error = passwordMismatchError;
                return View("Login");
            }

            // Get existing users from session
            var existingUsers = new List<UserRegistrationModel>();
            var usersJson = HttpContext.Session.GetString("RegisteredUsers");
            if (!string.IsNullOrEmpty(usersJson))
            {
                existingUsers = System.Text.Json.JsonSerializer.Deserialize<List<UserRegistrationModel>>(usersJson) ?? new List<UserRegistrationModel>();
            }

            // Check if user already exists
            if (existingUsers.Any(u => u.Email == model.Email))
            {
                var duplicateEmailError = "An account with this email already exists.";
                if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                    return Json(new { success = false, message = duplicateEmailError });
                ViewBag.Error = duplicateEmailError;
                return View("Login");
            }

            // Add new user
            existingUsers.Add(model);
            
            // Save back to session
            var updatedUsersJson = System.Text.Json.JsonSerializer.Serialize(existingUsers);
            HttpContext.Session.SetString("RegisteredUsers", updatedUsersJson);

            if (Request.Headers["Content-Type"].ToString().Contains("application/json"))
                return Json(new { success = true, message = "Account created successfully!" });
            ViewBag.Success = "Account created successfully! Please login.";
            return View("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}