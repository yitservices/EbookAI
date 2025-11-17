using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Services
{
    public class PlanFeaturesService : IPlanFeaturesService
    {
        private readonly ApplicationDbContext _context;
        public PlanFeaturesService(ApplicationDbContext context)
        {
            _context = context;
        }
        // ----- Features -----
        public async Task<IEnumerable<PlanFeatures>> GetAllFeaturesAsync()
        {
            return await _context.PlanFeatures
                .Where(f => (bool)f.IsActive)
                .OrderBy(f => f.FeatureId)
                .ToListAsync();
        }
        public async Task AddFeatureAsync(PlanFeatures feature)
        {
            _context.PlanFeatures.Add(feature);
            await _context.SaveChangesAsync();  // this actually commits to DB
        }
    }
}
