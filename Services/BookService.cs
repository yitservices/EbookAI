using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using EBookDashboard.Models.DTO;
using EBookDashboard.Models.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
namespace EBookDashboard.Services
{
    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Books?> GetBookByIdAsync(int bookId)
        {
            return await _context.Books.FindAsync(bookId);
        }
        // ----- Books -----
        public async Task<IEnumerable<Books>> GetAllBooksAsync()
        {
            return await _context.Books
                                 .Include(b => b.Chapters)
                                 .ToListAsync();
        }

        //public async Task<Books?> GetBookByIdAsync(int bookId)
        //{
        //    return await _context.Books
        //                         .Include(b => b.Chapters)
        //                         .FirstOrDefaultAsync(b => b.BookId == bookId);
        //}
        //public Task<BookDetailsDto?> GetBookDetailsAsync(int userId, int bookId)
        //{
        //    throw new NotImplementedException();
        //}
        //============================================
        // ----- Get Book Details with Chapters -----
        //============================================
        //public async Task<BookDetailsDto?> GetBookDetailsAsync(int userId, int bookId, int responseId)
        //{
        //    // It should be from APIRawResponse table
        //    return await _context.Books
        //        .Where(b => b.UserId == userId && b.BookId == bookId)
        //        .Select(b => new BookDetailsDto
        //        {
        //            BookId = b.BookId,
        //            Title = b.Title,
        //            Description = b.Description,
        //            Dedication= b.Dedication,
        //            Ghostwriting= b.Ghostwriting,
        //            Epigraph= b.Epigraph,
        //            CreatedAt = b.CreatedAt,
        //            Genre = b.Genre,
        //            Status=b.Status,
        //            TotalChapters = b.Chapters.Count,
        //            Chapters = b.Chapters
        //                .OrderBy(c => c.ChapterNumber)
        //                .Select(c => new ChapterDto
        //                {
        //                    ChapterNumber = c.ChapterNumber,
        //                    Title = c.Title,
        //                    Content = c.Content,
        //                    StatusCode = c.Status
        //                })
        //                .ToList()
        //        })
        //        .FirstOrDefaultAsync();
        //}
        //=======================================
        // Chapter Number Generation
        //=====================================
        public async Task<int> GetNextChapterNumberAsync(int userId, int bookId)
        {
            if (userId <= 0 || bookId <= 0)
            {
                Console.WriteLine($"❌ Invalid parameters: UserId={userId}, BookId={bookId}");
                return 1;
            }

            try
            {
                Console.WriteLine($"🔍 Getting next chapter number for User: {userId}, Book: {bookId}");

                // Get the highest chapter number from APIRawResponse for this user and book
                var maxChapter = await _context.APIRawResponse
                    .Where(r => r.UserId == userId && r.BookId == bookId)
                    .MaxAsync(r => (int?)r.Chapter);

                if (maxChapter.HasValue)
                {
                    int nextChapter = maxChapter.Value + 1;
                    Console.WriteLine($"✅ Found existing chapters. Max chapter: {maxChapter.Value}, Next chapter: {nextChapter}");
                    return nextChapter;
                }
                else
                {
                    Console.WriteLine($"✅ No chapters found. Starting with chapter 1");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting next chapter number: {ex.Message}");
                // Return 1 as default in case of error
                return 1;
            }
        }

        //==================================================
        // ----- Get Book Details from APIRawResponse -----
        //==================================================
        public async Task<BookDetailsDto?> GetBookDetailsFromRawDataAsync(int userId, int bookId)
        {
            var rawResponse = await _context.APIRawResponse
        .FirstOrDefaultAsync(b => b.UserId == userId && b.BookId == bookId);

            if (rawResponse == null)
                return null;

            // Parse chapter number safely
            int chapterNumber = 1;
            if (rawResponse.Chapter > 0)
            {
                chapterNumber = rawResponse.Chapter;
            }

            return new BookDetailsDto
            {
                BookId = rawResponse.BookId ?? 0,
                Title = rawResponse.Title ?? "Untitled Book",
                Description = "",
                Genre = "",
                TotalChapters = 1,
                Chapters = new List<ChapterDto>
                    {
                        new ChapterDto
                            {
                                ChapterNumber = chapterNumber,
                                Title = rawResponse.Title ?? "Untitled Chapter",
                                Content = rawResponse.ResponseData ?? "",
                                StatusCode = "Generated",
                                CreatedAt=rawResponse.CreatedAt
                            }
                    },
                RawResponseId = rawResponse.ResponseId,
                Endpoint = rawResponse.Endpoint ?? "",
                CreatedAt = rawResponse.CreatedAt,
            };
        }

        //=============================================
        // ----- Create Book from Request -----
        //=============================================
        public async Task<Books> CreateBookFromRequestAsync(CreateBookRequest request)
        {
            var book = new Books
            {
                AuthorId = request.AuthorId,
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                Title = request.Title.Trim(),
                Subtitle = request.Subtitle,
                AuthorCode = request.AuthorCode,
                BookCode = request.BookCode,
                LanguageId = request.LanguageId,
                CoverImagePath = request.CoverImagePath ?? "",
                ManuscriptPath = request.ManuscriptPath ?? "",
                Genre = request.Genre?.Trim() ?? "",
                Description = request.Description?.Trim(),
                WordCount = request.WordCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = BookStatus.Draft.ToString(),
                Dedication = request.Dedication?.Trim() ?? "",
                Ghostwriting = request.Ghostwriting?.Trim() ?? "",
                Epigraph = request.Epigraph?.Trim() ?? "",
            };

            return await CreateBookAsync(book);
        }
        // ----- to make BookId relationship Key -----
        public async Task<Books> CreateBookAsync(Books book)
        {
            // Set default values if not provided
            book.CreatedAt = DateTime.UtcNow;
            book.UpdatedAt = DateTime.UtcNow;
            book.Status = book.Status ?? BookStatus.Draft.ToString();

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return book;
        }
        //=======================================
        public async Task<bool> UpdateBookAsync(Books book)
        {
            book.UpdatedAt = DateTime.UtcNow;
            _context.Books.Update(book);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteBookAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return false;

            _context.Books.Remove(book);
            return await _context.SaveChangesAsync() > 0;
        }

        // ----- Categories -----
        public async Task<IEnumerable<Categories>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                                 .OrderBy(c => c.CategoryName)
                                 .ToListAsync();
        }

        // ----- Book Prices -----
        public async Task<BookPrice?> GetPriceByBookIdAsync(int bookId)
        {
            return await _context.BookPrices
                                 .FirstOrDefaultAsync(p => p.BookId == bookId);
        }

        public async Task<BookPrice> SetBookPriceAsync(BookPrice price)
        {
            _context.BookPrices.Add(price);
            await _context.SaveChangesAsync();
            return price;
        }

        public async Task<bool> UpdateBookPriceAsync(BookPrice price)
        {
            _context.BookPrices.Update(price);
            return await _context.SaveChangesAsync() > 0;
        }

        // ----- Book Versions -----
        public async Task<IEnumerable<BookVersion>> GetVersionsByBookIdAsync(int bookId)
        {
            return await _context.BookVersions
                                 .Where(v => v.BookId == bookId)
                                 .ToListAsync();
        }

        public async Task<BookVersion> AddBookVersionAsync(BookVersion version)
        {
            _context.BookVersions.Add(version);
            await _context.SaveChangesAsync();
            return version;
        }

        public async Task<BookVersion?> GetVersionByIdAsync(int versionId)
        {
            return await _context.BookVersions
                                 .FirstOrDefaultAsync(v => v.BookVersionId == versionId);
        }

        //Task<IEnumerable<Categories>> IBookService.GetAllCategoriesAsync()
        //{
        //    throw new NotImplementedException();
        //}
        public async Task<UserBooksViewModel> GetUserBooksWithChaptersAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var books = await _context.Books
                .Where(b => b.UserId == userId)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new UserBook
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description ?? "No description available",
                    Genre = b.Genre ?? "Uncategorized",
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    TotalChapters = b.Chapters.Count,
                    TotalWords = b.Chapters.Sum(c => c.Content != null ? CalculateWordCount(c.Content) : 0),
                    CoverImagePath = b.CoverImagePath ?? "/images/default-book-cover.jpg",
                    Chapters = b.Chapters
                        .OrderBy(c => c.ChapterNumber)
                        .Select(c => new UserChapter
                        {
                            ChapterId = c.ChapterId,
                            ChapterNumber = c.ChapterNumber,
                            Title = c.Title,
                            Content = c.Content,
                            Status = c.Status,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            WordCount = c.Content != null ? CalculateWordCount(c.Content) : 0,
                            PreviewContent = c.Content != null ? GetContentPreview(c.Content, CalculateWordCount(c.Content)) : "No content available"
                        })
                        .ToList()
                })
                .ToListAsync();

            return new UserBooksViewModel
            {
                UserId = userId,
                UserName = user?.FullName ?? "User",
                Books = books
            };
        }

        public async Task<UserBook?> GetUserBookDetailsAsync(int userId, int bookId)
        {
            return await _context.Books
                .Where(b => b.UserId == userId && b.BookId == bookId)
                .Include(b => b.Chapters)
                .Select(b => new UserBook
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description ?? "No description available",
                    Genre = b.Genre ?? "Uncategorized",
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    TotalChapters = b.Chapters.Count,
                    TotalWords = b.Chapters.Sum(c => CalculateWordCount(c.Content)),
                    CoverImagePath = b.CoverImagePath ?? "/images/default-book-cover.jpg",
                    Chapters = b.Chapters
                        .OrderBy(c => c.ChapterNumber)
                        .Select(c => new UserChapter
                        {
                            ChapterId = c.ChapterId,
                            ChapterNumber = c.ChapterNumber,
                            Title = c.Title,
                            Content = c.Content,
                            Status = c.Status,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            WordCount = c.Content != null ? CalculateWordCount(c.Content) : 0,
                            PreviewContent = GetContentPreview(c.Content, CalculateWordCount(c.Content))
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }
        public async Task<List<UserBook>> GetUserBooksSummaryAsync(int userId)
        {
            return await _context.Books
                .Where(b => b.UserId == userId)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new UserBook
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description ?? "No description available",
                    Genre = b.Genre ?? "Uncategorized",
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    TotalChapters = b.Chapters.Count,
                    TotalWords = b.Chapters.Sum(c => CalculateWordCount(c.Content)),
                    CoverImagePath = b.CoverImagePath ?? "/images/default-book-cover.jpg"
                })
                .ToListAsync();
        }
        private int CalculateWordCount(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            // Remove HTML tags for accurate word count
            var plainText = System.Text.RegularExpressions.Regex.Replace(
                content, "<.*?>", string.Empty);

            // Split by multiple whitespace characters and count non-empty words
            var wordCount = 0;
            var wordPattern = new System.Text.RegularExpressions.Regex(@"\b\w+\b");
            wordCount = wordPattern.Matches(plainText).Count;

            return wordCount;
        }

        private string GetContentPreview(string? content, int maxLength = 150)
        {
            if (string.IsNullOrEmpty(content))
                return "No content available";

            // Remove HTML tags for preview
            var plainText = System.Text.RegularExpressions.Regex.Replace(
                content, "<.*?>", string.Empty);

            return plainText.Length <= maxLength
                ? plainText
                : plainText.Substring(0, maxLength) + "...";
        }
     
        //=======================================
        // Get a single saved API response for a user and book
        //====================================
        public async Task<APIRawResponse?> GetLatestBookResponseAsync(int userId, int bookId)
        {
            // APIRawResponse table

            return await _context.APIRawResponse
                .Where(r => r.UserId == userId && r.BookId == bookId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }
        //==========================================
        //   Get all saved books for a user
        //==========================================
        public async Task<IEnumerable<SavedBookDto>> GetSavedBooksForDropdownAsync(int userId)
        {
            if (userId == 0)
                throw new ArgumentException("UserId is required.");

            return await _context.Books
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new SavedBookDto
                {
                    //ResponseId=b.ResponseId,
                    UserId = b.UserId,
                    BookId = b.BookId,
                    BookTitle = b.Title,
                    CreatedDate = b.CreatedAt
                })
                .ToListAsync();
        }
        
        //==========================================
        //      When Book Selected from Drop-Down
        //   Get all saved book's Chapter Nos
        //==========================================
        public async Task<IEnumerable<ChapterDto>> GetSavedBooksChaptersAsync(int userId, int bookId)
        {
            if (userId == 0)
                throw new ArgumentException("UserId is required.");

            var rawData = await _context.APIRawResponse
                .Where(b => b.UserId == userId && b.BookId == bookId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();  // Pull to memory to avoid EF GroupBy translation issue

            var latestChapters = rawData
                .GroupBy(b => b.Chapter)
                .Select(g => g.First())  // First item is already the latest because of ordering
                .OrderBy(c => c.Chapter)
                .ToList();

            return latestChapters.Select(b => new ChapterDto
            {
                ChapterNumber = b.Chapter,
                Title = b.Title,
                Content = b.ResponseData,
                StatusCode = b.StatusCode,
                CreatedAt = b.CreatedAt
            });

        }
        // Get latest BookId for a user
        public async Task<int?> GetLatestBookIdAsync(int userId)
        {
            if (userId == 0)
                throw new ArgumentException("UserId is required.");

            var latestBook = await _context.Books
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new { b.BookId })
                .FirstOrDefaultAsync();

            return latestBook?.BookId;
        }

        // Get latest book with details
        public async Task<SavedBookDto?> GetLatestBookAsync(int userId)
        {
            if (userId == 0)
                throw new ArgumentException("UserId is required.");

            return await _context.Books
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new SavedBookDto
                {
                    BookId = b.BookId,
                    BookTitle = b.Title,
                    CreatedDate = b.CreatedAt
                })
                .FirstOrDefaultAsync();
        }
        public async Task<(bool success, string data, string message)> GetBookApiResponseAsync(int userId, int bookId)
        {
            var response = await _context.APIRawResponse
                .Where(r => r.UserId == userId && r.BookId == bookId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (response == null)
                return (false, null, "No response found.");

            return (true, response.ResponseData, "Response retrieved successfully.");
        }
        // ====Book======================================= 100% OK
        // Load Selected Book from Drop-Down from Books table
        // ==============================================
        public async Task<BookDetailsResponseDto?> GetBookDetailsAsync(int userId, int bookId)
        {
            try
            {
                Console.WriteLine($"🔍 [Service] Loading book details for User: {userId}, Book: {bookId}");

                // Step 1: Get book info
                var book = await _context.Books
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.BookId == bookId);

                if (book == null)
                {
                    Console.WriteLine($"❌ [Service] Book {bookId} not found for user {userId}");
                    return null;
                }

                Console.WriteLine($"✅ [Service] Book found: {book.Title}");

                // Get chapters from APIRawResponse with user filtering
                var chapters = await _context.APIRawResponse
                    .Where(c => c.UserId == userId && c.BookId == bookId)
                    .OrderBy(c => c.Chapter)
                    .Select(c => new ChapterDto
                    {
                        ResponseId = c.ResponseId,
                        ChapterNumber = c.Chapter,
                        Title = c.Title ?? "Untitled Chapter",
                        RequestData = c.RequestData,
                        Content = c.ResponseData,
                        StatusCode = c.StatusCode ?? "Draft",
                        CreatedAt = c.CreatedAt,
                    }).ToListAsync();

                Console.WriteLine($"📚 [Service] Found {chapters.Count} chapters for book {bookId}");

                return new BookDetailsResponseDto
                {
                    Success = true,
                    BookId = book.BookId,
                    BookTitle = book.Title,
                    Description = book.Description,
                    Genre = book.Genre,
                    TotalChapters = chapters.Count,
                    Chapters = chapters
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Service] Error loading book details: {ex.Message}");
                return new BookDetailsResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        //=======================================
        // Get a single saved API response for a user and book
        //====================================
        public async Task<APIRawResponse?> GetSelectedBookResponseAsync(int userId, int bookId, int chapterNo)
        {
            // APIRawResponse table
            return await _context.APIRawResponse
                .Where(r => r.UserId == userId && r.BookId == bookId && r.Chapter == chapterNo)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }
        //======================================================
        //  Finalize Chapter: Add record + Update APIRawResponse
        //======================================================
        public async Task<bool> SetRecordReadOnlyAsync(FinalizeChapters finalize)
        {
            try
            {
                if (finalize == null || finalize.ResponseId <= 0)
                    return false;

                // 1. Insert new record in FinalizeChapter table
                await _context.FinalizeChapters.AddAsync(finalize);

                // 2. Update StatusCode in APIRawResponse table
                var rawResponse = await _context.APIRawResponse
                    .FirstOrDefaultAsync(r =>
                      r.ResponseId == finalize.ResponseId &&
                      r.UserId == finalize.UserId &&
                      r.BookId == finalize.BookId &&
                      r.Chapter == finalize.Chapter
                    );
                if (rawResponse == null)
                {
                    return false; // no matching record found
                }

                rawResponse.StatusCode = "ReadOnly";
                rawResponse.UpdatedAt = DateTime.Now;

                // Commit both operations (atomic)
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SetRecordReadOnlyAsync: {ex.Message}");
                return false;
            }
        }

        // ====Chapter==================================== 100% OK
        // Load Selected Chapter of Book from Drop-Down 
        // ==============================================
        public async Task<BookDetailsResponseDto?> GetBookDetailsAsync2(int userId, int bookId, int chapterNo)
        {
            try
            {
                Console.WriteLine($"🔍 [Service] Loading book details for User: {userId}, Book: {bookId}");

                // Step 1: Get book info
                var book = await _context.Books
                    .FirstOrDefaultAsync(b => b.UserId == userId && b.BookId == bookId);

                if (book == null)
                {
                    Console.WriteLine($"❌ [Service] Book {bookId} not found for user {userId}");
                    return null;
                }

                Console.WriteLine($"✅ [Service] Book found: {book.Title}");

                // Get chapters from APIRawResponse with user filtering
                if (chapterNo > 0)
                {
                    Console.WriteLine($"🔍 [Service] Filtering for Chapter: {chapterNo}");
                }
                var chapters = await _context.APIRawResponse
                    .Where(c => c.UserId == userId && c.BookId == bookId && c.Chapter== chapterNo)
                    .OrderByDescending(c => c.CreatedAt) 
                    .ThenBy(c => c.ResponseId) // Secondary order by Id
                    .Select(c => new ChapterDto
                    {
                        ResponseId = c.ResponseId,
                        ChapterNumber = c.Chapter,
                        Title = c.Title ?? "Untitled Chapter",
                        RequestData = c.RequestData,
                        Content = c.ResponseData,
                        StatusCode = c.StatusCode ?? "Draft",
                        CreatedAt = c.CreatedAt,
                    })
                    .ToListAsync();

                Console.WriteLine($"📚 [Service] Found {chapters.Count} chapters for book {bookId}");

                return new BookDetailsResponseDto
                {
                    Success = true,
                    BookId = book.BookId,
                    BookTitle = book.Title,
                    Description = book.Description,
                    Genre = book.Genre,
                    TotalChapters = chapters.Count,
                    Chapters = chapters
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Service] Error loading book details: {ex.Message}");
                return new BookDetailsResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
        //=========================================
        //   Last Chapter Number
        //=========================================
        public async Task<int> GetLastChapterAsync(int userId, int bookId)
        {
            var lastChapter = await _context.APIRawResponse
                .Where(x => x.UserId == userId && x.BookId == bookId && x.Chapter != null)
                .OrderByDescending(x => x.Chapter)
                .Select(x => x.Chapter)
                .FirstOrDefaultAsync();

            // If no chapter found, return 0 (or 1 if you want to start from Chapter 1)
            return lastChapter;
        }
        public List<string> ExtractChapterNames(string json)
        {
            // Deserialize only the part we need
            dynamic obj = JsonConvert.DeserializeObject(json);

            string htmlList = obj.data.suggest_chapter_name;

            List<string> chapters = new List<string>();

            // Extract <li>...</li> content using Regex
            MatchCollection matches = Regex.Matches(htmlList, @"<li>(.*?)<\/li>");

            foreach (Match match in matches)
            {
                chapters.Add(match.Groups[1].Value.Trim());
            }

            return chapters;
        }

        public Task<bool> SetRecordReadOnlyAsync(int responseId)
        {
            throw new NotImplementedException();
        }
    }

}