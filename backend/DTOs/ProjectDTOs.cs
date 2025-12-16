using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateProjectDto
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
    }
}