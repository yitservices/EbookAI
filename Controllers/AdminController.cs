﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using EBookDashboard.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Controllers
{
    [Authorize(Roles = "Admin")] // ✅ Only Admin users can access this controller
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IBookService _bookService;
        private readonly ApplicationDbContext _context;

        public AdminController(IUserService userService, IBookService bookService, ApplicationDbContext context)
        {
            _userService = userService;
            _bookService = bookService;
            _context = context;
        }
        public IActionResult Dashboard()
        {
            return View(); // Now uses the updated Dashboard.cshtml
        }
        
        public IActionResult DashboardNew()
        {
            return View(); // Views/Admin/DashboardNew.cshtml
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            var allUsers = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
            return View(allUsers);
        }

        // GET: /Admin/Roles
        public async Task<IActionResult> Roles()
        {
            var allRoles = await _context.Roles
                .Include(r => r.Users)
                .ToListAsync();
            return View(allRoles);
        }

        // GET: /Admin/Analytics
        public IActionResult Analytics()
        {
            return View();
        }

        // GET: /Admin/Settings
        public IActionResult Settings()
        {
            return View();
        }

        // GET: /Admin/Subscriptions
        public IActionResult Subscriptions()
        {
            return View();
        }

        // GET: /Admin/ContentManagement
        public async Task<IActionResult> ContentManagement()
        {
            var books = await _bookService.GetAllBooksAsync();
            return View(books);
        }

        // GET: /Admin/AuthorManagement
        public async Task<IActionResult> AuthorManagement()
        {
            var authors = await _context.Authors
                .Include(a => a.Books)
                .ToListAsync();
            return View(authors);
        }

        // GET: /Admin/UserPlanInfo
        public async Task<IActionResult> UserPlanInfo()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/BookOwnership
        public async Task<IActionResult> BookOwnership()
        {
            var books = await _context.Books
                .Include(b => b.Chapters)
                .ToListAsync();
            
            var authors = await _context.Authors.ToListAsync();
            ViewBag.Authors = authors;
            
            return View(books);
        }
    }
}
