namespace SeniorProject.Models
{
    public class SearchResultDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ChainName { get; set; } = string.Empty;
        public string TownName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? PromoPrice { get; set; }
        public string CleanName { get; set; } = string.Empty;
        public int ChainId { get; set; }
        public int TownId { get; set; }
    }
}
