using EBookDashboard.Interfaces;
using EBookDashboard.Models;
using EBookDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace EBookDashboard.Controllers
{
    [Route("Admin/[controller]")]
    public class PlanFeaturesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPlanFeaturesService _planFeaturesService;

        public PlanFeaturesController(ApplicationDbContext context, IPlanFeaturesService planFeaturesService)
        {
            _planFeaturesService = planFeaturesService;
            _context = context;
        }
        // ✅ This action serves the Razor View
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var pf = await _planFeaturesService.GetAllFeaturesAsync();
            return View("~/Views/Admin/PlanFeatures.cshtml", pf);
        }

        // ✅ Optional: JSON API for Vue.js
        [HttpGet("/api/planfeatures")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var pf = await _planFeaturesService.GetAllFeaturesAsync();
                // Ensure we return the data properly
                if (pf == null)
                {
                    return Ok(new List<PlanFeatures>());
                }
                return Ok(pf);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error loading features", message = ex.Message });
            }
        }
        [HttpPost("/api/planfeatures/add")]
        public async Task<IActionResult> AddFeature([FromBody] PlanFeatures model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { error = "Feature data is required" });
                }

                // Ensure required fields are set
                if (string.IsNullOrWhiteSpace(model.FeatureName))
                {
                    return BadRequest(new { error = "Feature name is required" });
                }

                // Set default values if not provided
                if (model.PlanId == null || model.PlanId == 0)
                {
                    model.PlanId = 0;
                }

                // Ensure IsActive is set (convert from boolean if needed)
                if (model.IsActive != 0 && model.IsActive != 1)
                {
                    model.IsActive = 1; // Default to active
                }

                // Set Status based on IsActive if Status is not provided
                if (string.IsNullOrWhiteSpace(model.Status))
                {
                    model.Status = model.IsActive == 1 ? "Active" : "Inactive";
                }

                // Set default currency if not provided
                if (string.IsNullOrWhiteSpace(model.Currency))
                {
                    model.Currency = "USD";
                }

                await _planFeaturesService.AddFeatureAsync(model);
                return Ok(new { success = true, message = "Feature added successfully", feature = model });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error adding feature", message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteFeature(int id)
        {
            var feature = await _context.PlanFeatures.FindAsync(id);
            if (feature == null)
                return NotFound(new { message = "Feature not found" });

            _context.PlanFeatures.Remove(feature);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Feature deleted successfully" });
        }

        // API route variant for SPA usage
        [HttpDelete("/api/planfeatures/{id}")]
        public async Task<ActionResult> ApiDeleteFeature(int id)
        {
            try
            {
                var feature = await _context.PlanFeatures.FindAsync(id);
                if (feature == null)
                    return NotFound(new { message = "Feature not found" });

                _context.PlanFeatures.Remove(feature);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Feature deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error deleting feature", message = ex.Message });
            }
        }

        [HttpPut("/api/planfeatures/{id}")]
        public async Task<IActionResult> UpdateFeature(int id, [FromBody] PlanFeatures model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { error = "Feature data is required" });
                }

                var feature = await _context.PlanFeatures.FindAsync(id);
                if (feature == null)
                {
                    return NotFound(new { error = "Feature not found" });
                }

                // Update properties
                feature.FeatureName = model.FeatureName;
                feature.Description = model.Description;
                feature.FeatureRate = model.FeatureRate;
                feature.Currency = model.Currency ?? feature.Currency;
                feature.IsActive = model.IsActive;
                feature.Status = model.IsActive == 1 ? "Active" : "Inactive";

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Feature updated successfully", feature = feature });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error updating feature", message = ex.Message });
            }
        }
    }

}
