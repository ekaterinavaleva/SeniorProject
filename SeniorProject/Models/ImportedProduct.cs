using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SeniorProject.Models
{
    public class ImportedProduct
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string ProductCode { get; set; }

        public string Category { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PromoPrice { get; set; }

        public DateTime ImportDate { get; set; } = DateTime.UtcNow;

        public int TownId { get; set; }
        public Town Town { get; set; }

        public int RetailChainId { get; set; }
        public RetailChain RetailChain { get; set; }
    }
}
