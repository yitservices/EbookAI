
using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Services
{
    public class BookService : IBookService
    {
        private readonly ApplicationDbContext _context;

        public BookService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----- Books -----
        public async Task<IEnumerable<Books>> GetAllBooksAsync()
        {
            return await _context.Books
                                 .Include(b => b.Chapters)
                                 .ToListAsync();
        }

        public async Task<Books?> GetBookByIdAsync(int bookId)
        {
            return await _context.Books
                                 .Include(b => b.Chapters)
                                 .FirstOrDefaultAsync(b => b.BookId == bookId);
        }

        public async Task<Books> CreateBookAsync(Books book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return book;
        }

        public async Task<bool> UpdateBookAsync(Books book)
        {
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
    }
}
