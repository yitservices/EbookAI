using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using EBookDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EBookDashboard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BooksController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IBookService _bookService;
        private readonly IAPIRawResponseService _rawResponseService;
        private readonly ApplicationDbContext _context;

        public BooksController(IBookService bookService, ApplicationDbContext context, IHttpClientFactory httpClientFactory, IAPIRawResponseService rawResponseService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _bookService = bookService;
            _rawResponseService = rawResponseService;
            _context = context;
        }
        // ✅ 1️⃣ — GET: Show the Razor view page
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AIGenerateBook()
        {
            return View(); // this will look for Views/Books/AIGenerateBook.cshtml
        }

        // ✅ 2️⃣ — POST: Call external API and return book data as JSON
        // Generate Book via API
        [AllowAnonymous]
        [HttpPost]
        [Route("Books/AIGenerateBook")]
        public async Task<IActionResult> AIGenerateBook([FromBody] AIBookRequest model)
        {
            if (model == null)
            {
                return BadRequest("Invalid request data");
            }
            string apiUrl = "http://162.229.248.26:8001/api/generate_chapter";
            string apiKey = "X-API-Key";
            string password = "AK-proj-c8r15p0EYc1B0SKi5_hP58HEyL6xP0ywmZ2hEpvpvU5y-i7yZ8IiyLv1K7cGSkyNh";
            var responseData = string.Empty;
            int? rawResponseId = null;
            try
            {
                using var client = new HttpClient();
                {
                    // Set timeout(optional but recommended)
                    client.Timeout = TimeSpan.FromMinutes(5);

                    client.DefaultRequestHeaders.Add(apiKey, password);

                    var json = JsonConvert.SerializeObject(model);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Console.WriteLine($"📤Sending request to API: {json}");

                    var response = await client.PostAsync(apiUrl, content);
                    responseData = await response.Content.ReadAsStringAsync();

                    // Log the API response in VS Output or console
                    Console.WriteLine($"📥API Response Status: {response.StatusCode}");
                    Console.WriteLine($"📥API Response Data: {responseData}");

                    // ✅ SAVE RAW RESPONSE FIRST
                    rawResponseId = await _rawResponseService.SaveRawResponseAsync(
                        model,
                        responseData,
                        apiUrl,
                        response.StatusCode.ToString()
                    );

                    Console.WriteLine($"📥 API Response Status: {response.StatusCode}");
                    Console.WriteLine($"📥 Raw Response saved with ID: {rawResponseId}");


                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode,
                            $"API error: {response.StatusCode} - {responseData}");
                    }
                    // Parse and save to database
                    try
                    {
                        // ✅ NEW: Parse the response and save to database
                        var apiResponse = JsonConvert.DeserializeObject<AIBookResponse>(responseData);
                        if (apiResponse != null)
                        {
                            Console.WriteLine($"🔍 API Response has content, proceeding to save...");
                            await SaveBookToDatabase(model, apiResponse, rawResponseId);
                            Console.WriteLine("✅ Book successfully saved to database");
                        }
                        else
                        {
                            Console.WriteLine("⚠️ No chapters to save to database");
                        }
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"⚠️ Database save failed but returning API response: {dbEx.Message}");
                        // Continue to return the API response even if DB save fails
                    }
                    return Content(responseData, "application/json");
                }
            }
            catch (HttpRequestException ex)
            {
                // Save error response
                if (rawResponseId == null)
                {
                    await _rawResponseService.SaveRawResponseAsync(
                        model,
                        responseData,
                        apiUrl,
                        "500",
                        $"Network error: {ex.Message}"
                    );
                }

                Console.WriteLine($"❌ Network error: {ex.Message}");
                return StatusCode(500, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Save error response
                if (rawResponseId == null)
                {
                    await _rawResponseService.SaveRawResponseAsync(
                        model,
                        responseData,
                        apiUrl,
                        "500",
                        $"Unexpected error: {ex.Message}"
                    );
                }

                // Log database errors but still return the book data to user
                Console.WriteLine($"Database save error: {ex.Message}");
                // You might want to return the API response even if saving fails
                return Content(responseData, "application/json");
            }
        }
        // ✅ NEW: Method to save book to database
        private async Task<bool> SaveBookToDatabase(AIBookRequest request, AIBookResponse apiResponse, int? rawResponseId = null)
        {
            Console.WriteLine("✅ Trying to save Book to database");

            if (apiResponse?.Data == null || string.IsNullOrEmpty(apiResponse.Data.Content))
            {
                Console.WriteLine("❌ No content data in API response to save");
                return false;
            }


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Convert UserId from string to int safely
                if (!int.TryParse(request.UserId, out int userId))
                {
                    userId = 1; // Default fallback
                }
                Console.WriteLine($"💾 Saving book to database - UserId: {userId} (converted from: {request.UserId})");

                // Create new Book
                var book = new Books
                {
                    Title = apiResponse.Title ?? $"Book - {request.UserInput}",
                    UserId = userId,
                    Status = "Generated",
                    CreatedAt = DateTime.UtcNow
                };
                Console.WriteLine($"🔍 Adding book to context: {book.Title}");
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                Console.WriteLine($"🔍 Book saved with ID: {book.BookId}");

                // Update raw response with the parsed book ID
                if (rawResponseId.HasValue)
                {
                    var rawResponse = await _context.APIRawResponse.FindAsync(rawResponseId.Value);
                    if (rawResponse != null)
                    {
                        rawResponse.ParsedBookId = book.BookId;
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"🔍 Updated raw response {rawResponseId} with book ID: {book.BookId}");
                    }
                }
                // Save the raw content as a single chapter (for now)
                var chapter = new Chapters
                {
                    BookId = book.BookId,
                    Title = "Full Book Content",
                    Content = apiResponse.Data.Content, // Save the entire raw content
                    ChapterNumber = 1
                };

                _context.Chapters.Add(chapter);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                Console.WriteLine($"✅ SUCCESS: Book saved to database with ID: {book.BookId}");

                // Create chapters
                //var chapters = new List<Chapters>();
                //for (int i = 0; i < apiResponse.Chapters.Count; i++)
                //{
                //    var chapterResponse = apiResponse.Chapters[i];
                //    chapters.Add(new Chapters
                //    {
                //        BookId = book.BookId,
                //        Title = chapterResponse.Title ?? $"Chapter {i + 1}",
                //        Content = chapterResponse.Content,
                //        ChapterNumber = i + 1
                //    });
                //}

                //_context.Chapters.AddRange(chapters);
                //await _context.SaveChangesAsync();

                //await transaction.CommitAsync();

                //Console.WriteLine($"✅ Book saved to database with ID: {book.BookId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error saving book to database: {ex.Message}");
                return false;
            }
        }


        // GET: Books
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Chapters)
                .Include(b => b.BookPrice)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Chapters)
                .Include(b => b.BookPrice)
                .FirstOrDefaultAsync(b => b.BookId == id);
            if (book == null) return NotFound();
            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Books book)
        {
            if (ModelState.IsValid)
            {
                book.CreatedAt = DateTime.UtcNow;
                book.Status = "Draft";
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Books book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    book.UpdatedAt = DateTime.UtcNow;
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books
                .Include(b => b.Chapters)
                .Include(b => b.BookPrice)
                .FirstOrDefaultAsync(b => b.BookId == id);
            if (book == null) return NotFound();
            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Books/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                book.Status = "Published";
                book.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Books/Archive/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                book.Status = "Archived";
                book.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
    }
}
