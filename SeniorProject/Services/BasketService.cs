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

        public async Task<List<string>> SearchAsync(string query, int? townId, CancellationToken cancellationToken = default)
        {
            var cleanQuery = query.ToCleanSortedString();
            var searchWords = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var baseQuery = _context.ImportedProducts.AsQueryable();
            if (townId.HasValue && townId.Value > 0)
            {
                baseQuery = baseQuery.Where(p => p.TownId == townId.Value);
            }
            
            // get the latest import date for this town before  string searches
            var latestDate = await baseQuery.MaxAsync(p => (DateTime?)p.ImportDate, cancellationToken);

            //  text search only on the most recent batch of products
            var sqlQuery = _context.ImportedProducts.AsQueryable();
            
            if (latestDate.HasValue)
            {
                sqlQuery = sqlQuery.Where(p => p.ImportDate == latestDate.Value);
            }
            if (townId.HasValue && townId.Value > 0)
            {
                sqlQuery = sqlQuery.Where(p => p.TownId == townId.Value); 
            }

            foreach (var word in searchWords)
            {
                sqlQuery = sqlQuery.Where(p => p.CleanName.Contains(word));
            }

            return await sqlQuery
                .Select(p => p.Name)
                .Distinct()
                .Take(20)
                .ToListAsync(cancellationToken);
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

            var basketDictionary = new Dictionary<int, BasketProductDetail>();
            foreach (var item in items)
            {
                string productName = item.ProductName.TrimStart('=', ',', '+', '.', '*', '-', ' ').Trim();
                string cleanName = productName.ToCleanSortedString();
                
                int searchHash = cleanName.GetStableHashCode();
                basketDictionary[searchHash] = item;
            }

            var searchHashes = basketDictionary.Keys.ToList();

            var allMatchedProducts = new List<ImportedProduct>();

            foreach (var chain in chainsInCity)
            {
                var latestDateForChain = await _context.ImportedProducts
                    .Where(p => p.TownId == townId && p.RetailChainId == chain.Id)
                    .MaxAsync(p => (DateTime?)p.ImportDate);

                if (latestDateForChain.HasValue)
                {
                    var products = await _context.ImportedProducts
                        .AsNoTracking()
                        .Where(p => p.TownId == townId && 
                                    p.RetailChainId == chain.Id && 
                                    p.ImportDate == latestDateForChain.Value &&
                                    searchHashes.Contains(p.NameHash))
                        .ToListAsync();

                    allMatchedProducts.AddRange(products);
                }
            }

            foreach (var chain in chainsInCity)
            {
                var chainResult = new BasketComparisonResult { RetailChainName = chain.Name, Products = new List<BasketProductDetail>() };
                var productsForThisChain = allMatchedProducts.Where(p => p.RetailChainId == chain.Id).ToList();

                
                foreach (var hash in searchHashes)
                {
                    var originalBasketItem = basketDictionary[hash];
                    
                    var bestProduct = productsForThisChain
                        .Where(p => p.NameHash == hash)
                        .OrderBy(p => p.PromoPrice ?? p.Price)
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

        public async Task<List<BasketComparisonResult>> ComparePredefinedBasketAsync(List<BasketProductDetail> items, int townId)
        {
            // this is for setting a timeout for real databases to skip for unit tests
            // same as previous code 
            if (_context.Database.IsRelational())
            {
                _context.Database.SetCommandTimeout(300);
            }
            var results = new List<BasketComparisonResult>();

            // this is for getting the existing chains in the chosen town
            // same as previous code 
            var chainsInCity = await _context.ImportedProducts
                .Where(p => p.TownId == townId)
                .Select(p => p.RetailChain)
                .Distinct()
                .ToListAsync();

            var categoryItems = new Dictionary<int, BasketProductDetail>();
            var categoryHashes = new Dictionary<int, List<int>>();

            // this is for fetching mapped names for each selected category
            // changed to get the mapped hashes for the category
            foreach (var item in items.Where(i => i.CategoryId.HasValue))
            {
                categoryItems[item.CategoryId.Value] = item;
                
                var mappedHashes = await _context.ProductGroupItems
                    .Where(pgi => pgi.ProductGroupId == item.CategoryId.Value)
                    .Select(pgi => pgi.RawProductId)
                    .ToListAsync();
                    
                categoryHashes[item.CategoryId.Value] = mappedHashes;
            }

            var allMappedHashes = categoryHashes.Values.SelectMany(x => x).Distinct().ToList();

            var allMatchedProducts = new List<ImportedProduct>();

            // this is for finding the most recent batch of products for each chain in the town
            // same as previous code 
            foreach (var chain in chainsInCity)
            {
                var latestDateForChain = await _context.ImportedProducts
                    .Where(p => p.TownId == townId && p.RetailChainId == chain.Id)
                    .MaxAsync(p => (DateTime?)p.ImportDate);

                if (latestDateForChain.HasValue)
                {
                    var products = await _context.ImportedProducts
                        .AsNoTracking()
                        .Where(p => p.TownId == townId && 
                                    p.RetailChainId == chain.Id && 
                                    p.ImportDate == latestDateForChain.Value &&
                                    allMappedHashes.Contains(p.NameHash))
                        .ToListAsync();

                    allMatchedProducts.AddRange(products);
                }
            }

            // this is for putting together the results and calculating the lowest price per chain
            // same as previous code but logic slightly changed to iterate over the grouping items instead of string hashes
            foreach (var chain in chainsInCity)
            {
                var chainResult = new BasketComparisonResult { RetailChainName = chain.Name, Products = new List<BasketProductDetail>() };
                var productsForThisChain = allMatchedProducts.Where(p => p.RetailChainId == chain.Id).ToList();
                
                foreach (var categoryPair in categoryHashes)
                {
                    var categoryId = categoryPair.Key;
                    var allowedHashes = categoryPair.Value;
                    var originalBasketItem = categoryItems[categoryId];
                    
                    var bestProduct = productsForThisChain
                        .Where(p => allowedHashes.Contains(p.NameHash))
                        .OrderBy(p => p.PromoPrice ?? p.Price)
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

            // this is for ordering the results to show stores that have the most desired items first
            // same as previous code 
            return results
                .OrderByDescending(r => r.Products.Count)
                .ThenBy(r => r.TotalPrice)
                .Take(3)
                .ToList();
        }
    }
}
