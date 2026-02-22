using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Services
{


    public class BasketService
    {
        private readonly ApplicationDbContext _context;

        public BasketService(ApplicationDbContext context)
        {
            _context = context;
        }

        //dropdown menu for user to enter data
        public async Task<List<string>> SearchAsync(string query)
        {
            return await _context.ImportedProducts
                .Where(p => p.Name.Contains(query))
                .Select(p => p.Name)
                .Distinct() 
                .Take(20) 
                .ToListAsync();
        }

        public async Task<List<BasketComparisonResult>> CompareBasketAsync(List<string> productNames, int townId)
        {
            // only set timeout for real databases, skips for unit tests
            if (_context.Database.IsRelational())
            {
                _context.Database.SetCommandTimeout(300);
            }
            var results = new List<BasketComparisonResult>();

            //get the existing chains in the chosen town
            var chainsInCity = await _context.ImportedProducts
                .Where(p => p.TownId == townId)
                .Select(p => p.RetailChain)
                .Distinct()
                .ToListAsync();

            foreach (var chain in chainsInCity)
            {
                var chainResult = new BasketComparisonResult { RetailChainName = chain.Name, Products = new List<BasketProductDetail>() };

                var latestDate = await _context.ImportedProducts
                    .Where(p => p.RetailChainId == chain.Id && p.TownId == townId)
                    .MaxAsync(p => p.ImportDate);

                var batchStartDate = latestDate.AddMinutes(-15);

                var storeProducts = await _context.ImportedProducts
                    .AsNoTracking()
                    .Where(p => p.RetailChainId == chain.Id && p.TownId == townId && p.ImportDate >= batchStartDate && p.ImportDate <= latestDate)
                    .ToListAsync();

                foreach (var itemName in productNames)
                {
                    var cleanSearch = itemName.Replace(".", " ").Replace(",", " ").Replace("-", " ").Replace("/", " ").Replace("*", " ").Replace("=", " ").Replace("+", " ").ToLowerInvariant();
                    var searchWords = cleanSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                    int threshold = (int)Math.Ceiling(searchWords.Length * 0.75);

                    var candidates = storeProducts
                        .Select(p => {
                            int matches = 0;
                            foreach(var sw in searchWords)
                            {
                                if (p.CleanName.Contains(sw, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches++;
                                }
                            }
                            var pTokens = p.CleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            double matchRatio = pTokens.Length > 0 ? (double)matches / pTokens.Length : 0;

                            return new { Product = p, MatchCount = matches, MatchRatio = matchRatio };
                        })
                        .Where(x => x.MatchCount >= threshold) 
                        .OrderByDescending(x => x.MatchRatio) 
                        .ThenByDescending(x => x.MatchCount) 
                        .ThenBy(x => x.Product.Price) 
                        .Take(3)
                        .ToList();

                    if (candidates.Any())
                    {
                        var best = candidates.First().Product;
                        
                        chainResult.Products.Add(new BasketProductDetail
                        {
                            ProductName = best.Name,
                            Price = best.PromoPrice ?? best.Price,
                            IsPromo = best.PromoPrice.HasValue
                        });
                        chainResult.TotalPrice += best.PromoPrice ?? best.Price;
                    }
                }
                results.Add(chainResult);
            }

            return results
                .OrderByDescending(r => r.Products.Count)
                .ThenBy(r => r.TotalPrice)
                .Take(3)
                .ToList();
        }
    }
}
