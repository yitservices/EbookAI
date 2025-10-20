using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookDashboard.Models
{
    public class AuthorPlans
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuthorPlanId { get; set; }

        // 🔹 Author and User Relation
        [Required]
        [ForeignKey("Author")]
        public int AuthorId { get; set; }

        [Required]
        public int UserId { get; set; }

        public Authors? Author { get; set; }   // Navigation property to Authors

        // 🔹 Plan Relation and Snapshot Data
        [Required]
        [ForeignKey("Plan")]
        public int PlanId { get; set; }

        public Plans? Plan { get; set; }       // Navigation property to Plans

        [Required]
        [StringLength(255)]
        public string PlanName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? PlanDescription { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PlanRate { get; set; } = 0.00m;

        [Required]
        public int PlanDays { get; set; } = 0;

        [Required]
        public int PlanHours { get; set; } = 0;

        [Required]
        public int MaxEBooks { get; set; } = 0;

        // 🔹 Timing and Status Fields
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = false;

        public bool TrialUsed { get; set; } = false;

        [StringLength(255)]
        public string? PaymentReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }
    }
}
