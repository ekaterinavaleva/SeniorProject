using System;
using System.Collections.Generic;

namespace SeniorProject.Models
{
    public class HomeViewModel
    {
        public Dictionary<string, List<PromoProductDto>> PromosByChain { get; set; } = new Dictionary<string, List<PromoProductDto>>();
        public DateTime? LastUpdatedDate { get; set; }
        
        public List<SearchResultDto> SearchResults { get; set; } = new List<SearchResultDto>();
        public List<string> AvailableCategories { get; set; } = new List<string>();
        public List<Town> AvailableTowns { get; set; } = new List<Town>();
        
        // Form states
        public string? ActiveStoreFilter { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedCategory { get; set; }
        public int? SelectedTownId { get; set; }
    }
}
