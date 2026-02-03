using System.ComponentModel.DataAnnotations;

namespace SeniorProject.Models
{
    public class Town
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }
    }
}
