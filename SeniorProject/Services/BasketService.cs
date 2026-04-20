using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SeniorProject.Data;
using SeniorProject.Models;
using SeniorProject.Extensions;

namespace SeniorProject.Services
{
    public class BasketService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public BasketService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<string>> SearchAsync(string query, int? townId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            {
                return new List<string>();
            }

            var cleanQuery = query.ToCleanSortedString();
            var searchWords = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (searchWords.Length == 0)
            {
                return new List<string>();
            }

            var cacheKey = townId.HasValue ? $"ProductNamesCache_Town_{townId.Value}" : "GlobalProductNamesCache";
            var allProductNames = await _cache.GetOrCreateAsync(cacheKey, async entry => 
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48);
                
                var queryDb = _context.ImportedProducts.AsQueryable();
                if (townId.HasValue) 
                {
                    queryDb = queryDb.Where(p => p.TownId == townId.Value);
                }

                var latestDate = await queryDb.MaxAsync(p => (DateTime?)p.ImportDate);
                if (latestDate.HasValue)
                {
                    queryDb = queryDb.Where(p => p.ImportDate == latestDate.Value);
                }

                var distinctProducts = await queryDb
                    .Select(p => new { p.Name, p.CleanName })
                    .Distinct()
                    .ToListAsync(cancellationToken);
                    
                return distinctProducts.Select(p => (p.Name, p.CleanName)).ToList();
            });

            if (allProductNames == null) return new List<string>();

            var filtered = allProductNames.AsEnumerable();
            foreach (var word in searchWords)
            {
                filtered = filtered.Where(p => p.CleanName.Contains(word, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.Take(20).Select(p => p.Name).Distinct().ToList();
        }

        public async Task<List<BasketComparisonResult>> CompareBasketAsync(List<BasketProductDetail> items, int townId)
        {
            // only set timeout for real databases, skips for unit tests
            if (_context.Database.IsRelational())
            {
                _context.Database.SetCommandTimeout(300);
            }
            var results = new List<BasketComparisonResult>();

            // distinct chain ids in the chosen town, then load the chains
            var chainIdsInCity = await _context.ImportedProducts
                .Where(p => p.TownId == townId)
                .Select(p => p.RetailChainId)
                .Distinct()
                .ToListAsync();

            var chainsInCity = await _context.RetailChains
                .Where(c => chainIdsInCity.Contains(c.Id))
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

            var chainIdsInCity = await _context.ImportedProducts
                .Where(p => p.TownId == townId)
                .Select(p => p.RetailChainId)
                .Distinct()
                .ToListAsync();

            var chainsInCity = await _context.RetailChains
                .Where(c => chainIdsInCity.Contains(c.Id))
                .ToListAsync();

            var categoryItems = new Dictionary<int, BasketProductDetail>();
            var categoryHashes = new Dictionary<int, List<int>>();

            // this is for fetching mapped names for each selected category
            // changed to get the mapped hashes for the category
            foreach (var item in items.Where(i => i.CategoryId.HasValue))
            {
                categoryItems[item.CategoryId.Value] = item;
                
                if (item.CategoryId.Value < 0) continue;

                var mappedHashes = await _context.ProductGroupItems
                    .Where(pgi => pgi.ProductGroupId == item.CategoryId.Value)
                    .Select(pgi => pgi.RawProductId)
                    .ToListAsync();
                    
                categoryHashes[item.CategoryId.Value] = mappedHashes;
            }

            var allMappedHashes = categoryHashes.Values.SelectMany(x => x).Distinct().ToList();
            var directCategoryIds = categoryItems.Keys.Where(id => id < 0).Select(id => Math.Abs(id).ToString()).ToList();

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
                                    (allMappedHashes.Contains(p.NameHash) || directCategoryIds.Contains(p.Category)))
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
                
                foreach (var itemPair in categoryItems)
                {
                    var categoryId = itemPair.Key;
                    var originalBasketItem = itemPair.Value;
                    ImportedProduct bestProduct = null;
                    
                    if (categoryId < 0)
                    {
                        string catCode = Math.Abs(categoryId).ToString();
                        bestProduct = productsForThisChain
                            .Where(p => p.Category == catCode)
                            .OrderBy(p => p.PromoPrice ?? p.Price)
                            .FirstOrDefault();
                    }
                    else if (categoryHashes.ContainsKey(categoryId))
                    {
                        var allowedHashes = categoryHashes[categoryId];
                        bestProduct = productsForThisChain
                            .Where(p => allowedHashes.Contains(p.NameHash))
                            .OrderBy(p => p.PromoPrice ?? p.Price)
                            .FirstOrDefault();
                    }

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
