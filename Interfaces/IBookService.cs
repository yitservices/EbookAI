using EBookDashboard.Models;

namespace EBookDashboard.Interfaces
{
    public interface IBookService
    {
        // ----- Books -----
        Task<IEnumerable<Books>> GetAllBooksAsync();
        Task<Books?> GetBookByIdAsync(int bookId);
        Task<Books> CreateBookAsync(Books book);
        Task<bool> UpdateBookAsync(Books book);
        Task<bool> DeleteBookAsync(int bookId);

        // ----- Book Prices -----
        Task<BookPrice?> GetPriceByBookIdAsync(int bookId);
        Task<BookPrice> SetBookPriceAsync(BookPrice price);
        Task<bool> UpdateBookPriceAsync(BookPrice price);

        // ----- Book Versions -----
        Task<IEnumerable<BookVersion>> GetVersionsByBookIdAsync(int bookId);
        Task<BookVersion> AddBookVersionAsync(BookVersion version);
        Task<BookVersion?> GetVersionByIdAsync(int versionId);
    }
}
