using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Extensions;
using SeniorProject.Models;
using Microsoft.Extensions.Caching.Memory;

namespace SeniorProject.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _logger = logger;
            _db = db;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        // isolate promotions logic
        // cache the data for the promo page for 1h
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Promotions(string? store)
        {
            _db.Database.SetCommandTimeout(300);

            var viewModel = new HomeViewModel();

            try
            {
                var recentDate = await _db.ImportedProducts.MaxAsync(p => (DateTime?)p.ImportDate) ?? DateTime.UtcNow;
                viewModel.LastUpdatedDate = recentDate;

                var promoGroups = await _cache.GetOrCreateAsync("PromosByChain_Cache", async entry => 
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    var promosQuery = await _db.ImportedProducts
                        .AsNoTracking()
                        .Include(p => p.RetailChain)
                        .Where(p => p.ImportDate == recentDate && p.PromoPrice != null)
                        .ToListAsync();

                    var knownChains = new[] {
                        "Kaufland", "Lidl", "Billa", "T-Market", "Fantastico",
                        "Кауфланд", "Лидл", "Билла", "Т-Маркет", "Т Маркет", "Фантастико", "T Market"
                    };

                    return promosQuery
                        .Where(p => p.RetailChain != null && knownChains.Any(c => p.RetailChain.Name.Contains(c, StringComparison.OrdinalIgnoreCase)))
                        .GroupBy(p => knownChains.First(c => p.RetailChain.Name.Contains(c, StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(
                            g => g.Key,
                            g => g.GroupBy(p => p.Name)
                                  .Select(pg => pg.First())
                                  .Select(p => new PromoProductDto
                                  {
                                      ProductName = p.Name,
                                      PromoPrice = p.PromoPrice!.Value,
                                      RegularPrice = p.Price,
                                      PercentDiscount = p.Price > 0 ? Math.Round((1 - (double)(p.PromoPrice.Value / p.Price)) * 100) : 0,
                                  })
                            .OrderByDescending(p => p.PercentDiscount)
                            .Take(50)
                            .ToList()
                        );
                });

                viewModel.PromosByChain = promoGroups;

                if (string.IsNullOrEmpty(store) && promoGroups.Keys.Any())
                {
                    viewModel.ActiveStoreFilter = promoGroups.ContainsKey("Kaufland") ? "Kaufland" : promoGroups.Keys.First();
                }
                else
                {
                    viewModel.ActiveStoreFilter = store;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions page.");
                return View(new HomeViewModel()); 
            }
        }

        // same approach as the custom basket comparison
        // resolve names from cached dictionaries
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> ProductSearch(string? q, int? townId)
        {
            var viewModel = new HomeViewModel
            {
                SearchQuery = q,
                SelectedTownId = townId
            };

            try
            {
                var allTowns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
                viewModel.AvailableTowns = allTowns
                    .Where(t => t.Name.Any(char.IsLetter)
                             && t.Name != "Blagoevgrad"
                             && t.Name != "Благоевград")
                    .ToList();

                ViewBag.Query = q;
                ViewBag.TownId = townId;

                // town is required — same as basket comparison
                if (!townId.HasValue || string.IsNullOrWhiteSpace(q))
                {
                    return View(viewModel);
                }

                // cache small lookup dictionaries for town and chain names (no Include needed)
                var townNames = await _cache.GetOrCreateAsync("TownNamesDict", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48);
                    return await _db.Towns.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Name);
                });

                var chainNames = await _cache.GetOrCreateAsync("ChainNamesDict", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48);
                    return await _db.RetailChains.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name);
                });

                // cache products for this town 
                var cacheKey = $"ProductSearchCache_Town_{townId.Value}";

                var cachedProducts = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48);

                    var dbQuery = _db.ImportedProducts
                        .AsNoTracking()
                        .Where(p => p.TownId == townId.Value);

                    var latestDate = await dbQuery.MaxAsync(p => (DateTime?)p.ImportDate);
                    if (latestDate.HasValue)
                    {
                        dbQuery = dbQuery.Where(p => p.ImportDate == latestDate.Value);
                    }

                    // select only the columns needed
                    return await dbQuery
                        .Select(p => new SearchResultDto
                        {
                            ProductName = p.Name,
                            ChainId = p.RetailChainId,
                            TownId = p.TownId,
                            Category = p.Category,
                            Price = p.Price,
                            PromoPrice = p.PromoPrice,
                            CleanName = p.CleanName
                        })
                        .ToListAsync();
                });

                if (cachedProducts == null)
                {
                    return View(viewModel);
                }

                var cleanQuery = q.ToCleanSortedString();
                var searchWords = cleanQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                IEnumerable<SearchResultDto> filtered = cachedProducts;
                foreach (var word in searchWords)
                {
                    filtered = filtered.Where(p =>
                        !string.IsNullOrEmpty(p.CleanName) &&
                        p.CleanName.Contains(word, StringComparison.OrdinalIgnoreCase));
                }

                // keep only the cheapest product per chain
                var deduped = filtered
                    .GroupBy(p => p.ChainId)
                    .Select(g => g.OrderBy(p => p.PromoPrice ?? p.Price).First())
                    .OrderBy(p => p.PromoPrice ?? p.Price)
                    .Take(200)
                    .ToList();

                // give names from cached dictionaries
                foreach (var item in deduped)
                {
                    item.ChainName = chainNames != null && chainNames.TryGetValue(item.ChainId, out var cn) ? cn : "";
                    item.TownName = townNames != null && townNames.TryGetValue(item.TownId, out var tn) ? tn : "";
                }

                viewModel.SearchResults = deduped;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product search page.");
                return View(new HomeViewModel()); 
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet]
        public async Task<IActionResult> DebugChains()
        {
            var chains = await _db.RetailChains.Select(c => new { c.Id, c.Name }).ToListAsync();
            return Json(chains);
        }

        public IActionResult Privacy()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
