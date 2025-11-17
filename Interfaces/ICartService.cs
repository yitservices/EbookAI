using EBookDashboard.Models;

namespace EBookDashboard.Interfaces
{
    public interface ICartService
    {
        Task<PaymentSummaryViewModel> GetSummaryAsync();
        Task<CartUpdateResult> AddAsync(int featureId);
        Task<CartUpdateResult> RemoveAsync(int featureId);
        Task ClearAsync();
    }
}



