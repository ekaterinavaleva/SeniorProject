using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeniorProject.Models
{
    public class SavedBasket
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        public DateTime SavedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string WinningSupermarket { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPrice { get; set; }
        
        public List<SavedBasketItem> Items { get; set; } = new List<SavedBasketItem>();
        
        public int? TownId { get; set; }
        public Town Town { get; set; }
    }
}
