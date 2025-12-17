using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
    }

    public class CreateProjectDto
    {
        [Required]
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}