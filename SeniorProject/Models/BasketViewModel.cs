namespace SeniorProject.Models
{
    public class BasketComparisonResult
    {
        public string RetailChainName { get; set; }
        public decimal TotalPrice { get; set; }
        public List<BasketProductDetail> Products { get; set; }
    }

    public class BasketProductDetail
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public bool IsPromo { get; set; }
    }
}
