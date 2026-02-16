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
                    .Where(p => p.RetailChainId == chain.Id && p.TownId == townId && p.ImportDate >= batchStartDate && p.ImportDate <= latestDate)
                    .ToListAsync();

                foreach (var itemName in productNames)
                {
                    // logic for matching needs update, currently testing with exact words
                    var searchWords = itemName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                    var match = storeProducts
                        .Where(p => searchWords.All(w => p.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(p => p.Price)
                        .FirstOrDefault();

                    if (match != null)
                    {
                        chainResult.Products.Add(new BasketProductDetail
                        {
                            ProductName = match.Name,
                            Price = match.PromoPrice ?? match.Price,
                            IsPromo = match.PromoPrice.HasValue
                        });
                        chainResult.TotalPrice += match.PromoPrice ?? match.Price;
                    }
                }
                results.Add(chainResult);
            }

            return results.OrderBy(r => r.TotalPrice).Take(3).ToList();
        }
    }
}
