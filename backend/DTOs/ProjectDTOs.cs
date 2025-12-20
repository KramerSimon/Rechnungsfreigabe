using System.ComponentModel.DataAnnotations;
using RechnungsfreigabeAPI.Models;

namespace backend.DTOs
{
    public class ProjectDto
    {
        public string Id { get; set; } = string.Empty;
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string CostCenterId { get; set; } = string.Empty;
        public string? CostCenterName { get; set; }
        public decimal Budget { get; set; }
        public decimal SpentAmount { get; set; }
        public string Status { get; set; } = ProjectStatus.Geplant.ToString();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ProjectManagerId { get; set; }
    }

    public class CreateProjectDto
    {
        [Required]
        [StringLength(20)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string CostCenterId { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Budget { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal SpentAmount { get; set; } = 0;

        [StringLength(50)]
        public string Status { get; set; } = ProjectStatus.Geplant.ToString();

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? ProjectManagerId { get; set; }
    }
}