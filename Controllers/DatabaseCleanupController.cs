using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EBookDashboard.Models;
using Microsoft.AspNetCore.Authorization;

namespace EBookDashboard.Controllers
{
    [Route("Admin/[controller]")]
    [Authorize]
    public class DatabaseCleanupController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatabaseCleanupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /Admin/DatabaseCleanup/DropUserPreferencesAndFeatures
        [HttpPost]
        public async Task<IActionResult> DropUserPreferencesAndFeatures()
        {
            try
            {
                // Check if tables exist first
                var userPrefExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'UserPreferences'"
                ).FirstOrDefaultAsync();
                
                var userFeatExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'UserFeatures'"
                ).FirstOrDefaultAsync();

                if (userPrefExists > 0)
                {
                    // Drop foreign key constraints for UserPreferences
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE `UserPreferences` DROP FOREIGN KEY `FK_UserPreferences_Users_UserId`");
                    }
                    catch (Exception ex)
                    {
                        // Foreign key might not exist or have different name, continue
                        System.Diagnostics.Debug.WriteLine($"Could not drop FK for UserPreferences: {ex.Message}");
                    }

                    // Drop indexes for UserPreferences
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DROP INDEX `IX_UserPreferences_UserId_Key` ON `UserPreferences`");
                    }
                    catch { }

                    // Drop UserPreferences table
                    await _context.Database.ExecuteSqlRawAsync("DROP TABLE `UserPreferences`");
                }

                if (userFeatExists > 0)
                {
                    // Drop foreign key constraints for UserFeatures
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE `UserFeatures` DROP FOREIGN KEY `FK_UserFeatures_Features_FeatureId`");
                    }
                    catch { }

                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE `UserFeatures` DROP FOREIGN KEY `FK_UserFeatures_AuthorPlans_AuthorPlanId`");
                    }
                    catch { }

                    // Drop indexes for UserFeatures
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DROP INDEX `IX_UserFeatures_FeatureId` ON `UserFeatures`");
                    }
                    catch { }

                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DROP INDEX `IX_UserFeatures_AuthorPlanId` ON `UserFeatures`");
                    }
                    catch { }

                    // Drop UserFeatures table
                    await _context.Database.ExecuteSqlRawAsync("DROP TABLE `UserFeatures`");
                }

                return Json(new { 
                    success = true, 
                    message = "Tables UserPreferences and UserFeatures have been successfully deleted from the database." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error deleting tables: {ex.Message}",
                    details = ex.ToString()
                });
            }
        }

        // GET: /Admin/DatabaseCleanup/CheckTables
        [HttpGet]
        public async Task<IActionResult> CheckTables()
        {
            try
            {
                var tables = new List<string>();
                
                // Check if UserPreferences exists
                var userPrefExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'UserPreferences'"
                ).FirstOrDefaultAsync();
                
                // Check if UserFeatures exists
                var userFeatExists = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'UserFeatures'"
                ).FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    userPreferencesExists = userPrefExists > 0,
                    userFeaturesExists = userFeatExists > 0,
                    message = userPrefExists > 0 || userFeatExists > 0 
                        ? "Tables still exist in database" 
                        : "Tables have been deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error checking tables: {ex.Message}" 
                });
            }
        }
    }
}

