using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace EBookDashboard.Interfaces
{
    public interface IPlanFeaturesService
    {
        Task<IEnumerable<PlanFeatures>> GetAllFeaturesAsync();
        Task AddFeatureAsync(PlanFeatures feature);
    }
}
