using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookDashboard.Models
{
    [Table("planfeatures")]
    public class PlanFeatures
    {
        [Key]
        [Column("FeatureId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FeatureId { get; set; }

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

        [NotMapped]
        public decimal OriginalPrice { get; set; } = 0.00m;

        [Column("Currency")]
        [StringLength(12)]
        public string? Currency { get; set; }

        [Column("Status")]
        [StringLength(12)]
        public string? Status { get; set; }

        [Column("isActive")]
        public bool? IsActive { get; set; }

        [NotMapped]
        public string? IconClass { get; set; }

        [NotMapped]
        public string? DeliveryTime { get; set; }

        [NotMapped]
        public string? Revisions { get; set; }

        // Navigation properties
        public virtual ICollection<AuthorPlanFeatures>? AuthorPlanFeatures { get; set; }

        // Benefits as a computed property (in a real app, this might come from a separate table)
        [NotMapped]
        public List<string> Benefits 
        { 
            get 
            {
                var benefits = new List<string>();
                if (!string.IsNullOrEmpty(Description))
                {
                    // Simple implementation - in a real app, this would come from a separate benefits table
                    benefits.Add(Description);
                }
                return benefits;
            } 
        }
    }
}