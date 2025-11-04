using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EBookDashboard.Services
{
    public class FeatureAccessService
    {
        private readonly ApplicationDbContext _context;

        public FeatureAccessService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check if user has paid for a specific feature
        /// </summary>
        public async Task<bool> HasPaidFeatureAsync(int userId, int featureId)
        {
            var hasPaid = await _context.AuthorPlanFeaturesSet
                .AnyAsync(f => (f.UserId == userId || f.AuthorId == userId) 
                            && f.FeatureId == featureId 
                            && f.Status == "Paid" 
                            && f.IsActive == 1);
            
            return hasPaid;
        }

        /// <summary>
        /// Check if user has paid for any formatting features
        /// </summary>
        public async Task<bool> HasPaidFormattingFeaturesAsync(int userId)
        {
            var formattingFeatureNames = new[] { "Bold/Underline Text", "Custom Fonts", "Text Formatting Suite", 
                                                  "Paragraph Styling", "Export to PDF", "Premium Writing Suite" };
            
            var hasPaid = await _context.AuthorPlanFeaturesSet
                .AnyAsync(f => (f.UserId == userId || f.AuthorId == userId)
                            && f.Status == "Paid"
                            && f.IsActive == 1
                            && f.FeatureName != null
                            && formattingFeatureNames.Contains(f.FeatureName));
            
            return hasPaid;
        }

        /// <summary>
        /// Get all paid features for a user
        /// </summary>
        public async Task<List<AuthorPlanFeatures>> GetPaidFeaturesAsync(int userId)
        {
            return await _context.AuthorPlanFeaturesSet
                .Where(f => (f.UserId == userId || f.AuthorId == userId)
                         && f.Status == "Paid"
                         && f.IsActive == 1)
                .ToListAsync();
        }

        /// <summary>
        /// Check if user has access to PDF export feature
        /// </summary>
        public async Task<bool> CanExportPdfAsync(int userId)
        {
            return await HasPaidFeatureAsync(userId, 0) || await HasPaidFormattingFeaturesAsync(userId);
        }
    }
}

