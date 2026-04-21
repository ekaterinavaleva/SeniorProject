namespace SeniorProject.Models
{
    public class BasketComparisonResult
    {
        public string RetailChainName { get; set; }
        public string StoreAddress { get; set; }
        public decimal TotalPrice { get; set; }
        public List<BasketProductDetail> Products { get; set; }
    }

    public class BasketProductDetail
    {
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public bool IsPromo { get; set; }
        public int Quantity { get; set; } = 1;
        public int? CategoryId { get; set; }
    }

    public class CompareRequest
    {
        public List<BasketProductDetail> Items { get; set; }
        public int TownId { get; set; }
        //  if the comparison uses predefined data mapped by the retail manager
        public bool IsPredefined { get; set; }
    }

    public class SaveBasketRequest
    {
        public string WinningSupermarket { get; set; }  
        public string DisplayChainName { get; set; }    
        public decimal TotalPrice { get; set; }
        public int? TownId { get; set; }
        public List<BasketProductDetail> Items { get; set; }
    }
}
