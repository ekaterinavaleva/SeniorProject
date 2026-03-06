using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;
using SeniorProject.Extensions;

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
        public async Task<List<string>> SearchAsync(string query, int? townId)
        {
            var cleanQuery = query.ToCleanSortedString();
            var searchWords = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var sqlQuery = _context.ImportedProducts.AsQueryable();

            foreach (var word in searchWords)
            {
                sqlQuery = sqlQuery.Where(p => p.CleanName.Contains(word));
            }
            
            if (townId.HasValue && townId.Value > 0)
            {
                sqlQuery = sqlQuery.Where(p => p.TownId == townId.Value); 
            }

            return await sqlQuery
                .Select(p => p.Name)
                .Distinct() 
                .Take(20) 
                .ToListAsync();
        }

        public async Task<List<BasketComparisonResult>> CompareBasketAsync(List<BasketProductDetail> items, int townId)
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

            var latestDate = await _context.ImportedProducts
                .Where(p => p.TownId == townId)
                .MaxAsync(p => (DateTime?)p.ImportDate) ?? DateTime.UtcNow;

            var batchStartDate = latestDate.AddMinutes(-15);

            var basketDictionary = new Dictionary<int, BasketProductDetail>();
            foreach (var item in items)
            {
                string productName = item.ProductName.TrimStart('=', ',', '+', '.', '*', '-', ' ').Trim();
                string cleanName = productName.ToCleanSortedString();
                
                int searchHash = cleanName.GetStableHashCode();
                basketDictionary[searchHash] = item;
            }

            var searchHashes = basketDictionary.Keys.ToList();

            var allMatchedProducts = await _context.ImportedProducts
                .AsNoTracking()
                .Where(p => p.TownId == townId && 
                            p.ImportDate >= batchStartDate && 
                            p.ImportDate <= latestDate &&
                            searchHashes.Contains(p.NameHash))
                .ToListAsync();

            foreach (var chain in chainsInCity)
            {
                var chainResult = new BasketComparisonResult { RetailChainName = chain.Name, Products = new List<BasketProductDetail>() };
                var productsForThisChain = allMatchedProducts.Where(p => p.RetailChainId == chain.Id).ToList();

                
                foreach (var hash in searchHashes)
                {
                    var originalBasketItem = basketDictionary[hash];
                    
                    var bestProduct = productsForThisChain
                        .Where(p => p.NameHash == hash)
                        .OrderBy(p => p.Price)
                        .FirstOrDefault();

                    if (bestProduct != null)
                    {
                        decimal finalPrice = bestProduct.PromoPrice ?? bestProduct.Price;
                        decimal totalItemPrice = finalPrice * originalBasketItem.Quantity;

                        chainResult.Products.Add(new BasketProductDetail
                        {
                            ProductName = bestProduct.Name,
                            Price = totalItemPrice,
                            IsPromo = bestProduct.PromoPrice.HasValue,
                            Quantity = originalBasketItem.Quantity
                        });
                        chainResult.TotalPrice += totalItemPrice;
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
