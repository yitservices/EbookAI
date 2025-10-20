using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookDashboard.Models
{
    [Table("AuthorPlanFeatures")]
    public class AuthorPlanFeatures
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("AuthorId")]
        public int AuthorId { get; set; }

        [Column("UserId")]
        public int UserId { get; set; } = 0;

        [Column("UserEmail")]
        [StringLength(145)]
        public string? UserEmail { get; set; }

        [Column("FeatureId")]
        public int? FeatureId { get; set; }

        [Column("PlanId")]
        public int? PlanId { get; set; }

        [Column("FeatureName")]
        [StringLength(45)]
        public string? FeatureName { get; set; }

        [Column("Description")]
        [StringLength(245)]
        public string? Description { get; set; }

        [Column("FeatureRate", TypeName = "decimal(10,2)")]
        public decimal FeatureRate { get; set; } = 0.00m;

        [Column("Currency")]
        [StringLength(12)]
        public string? Currency { get; set; }

        [Column("TotalAmount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        [Column("PaymentReference")]
        [StringLength(255)]
        public string? PaymentReference { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("CancelledAt")]
        public DateTime? CancelledAt { get; set; }

        [Column("CancellationReason")]
        [StringLength(500)]
        public string? CancellationReason { get; set; }

        [Column("Status")]
        [StringLength(12)]
        public string? Status { get; set; }

        [Column("isActive")]
        public int? IsActive { get; set; }

        // ✅ Navigation Property (Many-to-One)
        [ForeignKey("FeatureId")]
        public PlanFeatures? PlanFeature { get; set; }
    }
}
