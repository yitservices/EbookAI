
using EBookDashboard.Models;
using Microsoft.EntityFrameworkCore;

public interface IPlanService
{
    Task<IEnumerable<Plans>> GetActivePlansAsync();
    Task<bool> CanPublishAsync(int planId, int currentEBooks);
}
public class PlanService : IPlanService
{
    private readonly ApplicationDbContext _context;
    public PlanService(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Plans>> GetActivePlansAsync() =>
        await _context.Plans.Where(p => p.IsActive).ToListAsync();

    public async Task<bool> CanPublishAsync(int planId, int currentEBooks)
    {
        var plan = await _context.Plans.FindAsync(planId);
        return plan != null && currentEBooks < plan.MaxEBooks;
    }
}
