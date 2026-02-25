using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeniorProject.Models
{
    public class SavedBasketItem
    {
        public int Id { get; set; }
        public int SavedBasketId { get; set; }
        public SavedBasket SavedBasket { get; set; }
        
        [Required]
        public string ProductName { get; set; }
        
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }
    }
}
