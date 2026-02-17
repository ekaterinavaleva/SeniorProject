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
            _context.Database.SetCommandTimeout(300);
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

                //find most recent time store data, to avoid old prices
                var latestDate = await _context.ImportedProducts
                    .Where(p => p.RetailChainId == chain.Id && p.TownId == townId)
                    .MaxAsync(p => p.ImportDate);

                var batchStartDate = latestDate.AddMinutes(-15);

                var storeProducts = await _context.ImportedProducts
                    .Where(p => p.RetailChainId == chain.Id && p.TownId == townId && p.ImportDate >= batchStartDate && p.ImportDate <= latestDate)
                    .ToListAsync();

                foreach (var itemName in productNames)
                {
                    // cleanup the search
                    var cleanName = itemName.Replace(".", " ").Replace(",", " ").Replace("-", " ").Replace("/", " ");
                    var searchWords = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                    // must match at least 75% of the words to avoid mineral water bankya to appear as mineral water devin
                    int threshold = (int)Math.Ceiling(searchWords.Length * 0.75);

                    var candidates = storeProducts
                        .Select(p => {
                            // same for products themselves (some are written as min. instead of mineral)
                            var pClean = p.Name.Replace(".", " ").Replace(",", " ").Replace("-", " ").Replace("/", " ");
                            var pTokens = pClean.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            
                            int matches = 0;
                            foreach(var sw in searchWords)
                            {
                                // check if mineralna starts with min
                                bool hit = pTokens.Any(pt => 
                                    pt.StartsWith(sw, StringComparison.OrdinalIgnoreCase) || 
                                    sw.StartsWith(pt, StringComparison.OrdinalIgnoreCase) || 
                                    pt.Equals(sw, StringComparison.OrdinalIgnoreCase));
                                
                                if(hit) matches++;
                            }

                            return new { Product = p, MatchCount = matches };
                        })
                        .Where(x => x.MatchCount >= threshold) 
                        .OrderByDescending(x => x.MatchCount) 
                        .ThenBy(x => x.Product.Price) 
                        .Take(3)
                        .ToList();

                    if (candidates.Any())
                    {
                        var best = candidates.First();
                        
                        chainResult.Products.Add(new BasketProductDetail
                        {
                            ProductName = best.Product.Name,
                            Price = best.Product.PromoPrice ?? best.Product.Price,
                            IsPromo = best.Product.PromoPrice.HasValue
                        });
                        chainResult.TotalPrice += best.Product.PromoPrice ?? best.Product.Price;
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
