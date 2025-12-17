using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Project
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}