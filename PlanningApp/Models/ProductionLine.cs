using System.ComponentModel.DataAnnotations;

namespace PlanningApp.Models
{
    public class ProductionLine
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Line name is required.")]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
