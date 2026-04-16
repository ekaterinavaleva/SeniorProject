namespace SeniorProject.Models
{
    public class PromoProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal PromoPrice { get; set; }
        public decimal RegularPrice { get; set; }
        public double PercentDiscount { get; set; }
    }
}
