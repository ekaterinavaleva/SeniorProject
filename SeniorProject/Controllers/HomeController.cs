using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index(string? q, int? townId, string? category, string? store)
        {


            _db.Database.SetCommandTimeout(300);

            var viewModel = new HomeViewModel
            {
                SearchQuery = q,
                SelectedCategory = category,
                SelectedTownId = townId
            };

            try
            {
                viewModel.AvailableTowns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
                viewModel.AvailableCategories = await _db.ImportedProducts
                                                  .Where(p => !string.IsNullOrEmpty(p.Category))
                                                  .Select(p => p.Category)
                                                  .Distinct()
                                                  .OrderBy(c => c)
                                                  .ToListAsync();

                var recentDate = await _db.ImportedProducts.MaxAsync(p => (DateTime?)p.ImportDate) ?? DateTime.UtcNow;
                viewModel.LastUpdatedDate = recentDate;

                var promosQuery = await _db.ImportedProducts
                    .AsNoTracking()
                    .Include(p => p.RetailChain)
                    .Where(p => p.ImportDate == recentDate && p.PromoPrice != null)
                    .ToListAsync();

                var knownChains = new[] {
                    "Kaufland", "Lidl", "Billa", "T-Market", "Fantastico",
                    "Кауфланд", "Лидл", "Билла", "Т-Маркет", "Т Маркет", "Фантастико", "T Market"
                };

                var promoGroups = promosQuery
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

                viewModel.PromosByChain = promoGroups;

                if (string.IsNullOrEmpty(store) && promoGroups.Keys.Any())
                {
                    viewModel.ActiveStoreFilter = promoGroups.ContainsKey("Kaufland") ? "Kaufland" : promoGroups.Keys.First();
                }
                else
                {
                    viewModel.ActiveStoreFilter = store;
                }

                var query = _db.ImportedProducts
                    .AsNoTracking() // this is because the query times out otherwise
                    .Include(p => p.Town)
                    .Include(p => p.RetailChain)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(q))
                {
                    query = query.Where(p => p.Name.Contains(q) || p.Category.Contains(q));
                }

                if (townId.HasValue)
                {
                    query = query.Where(p => p.TownId == townId);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                ViewBag.Query = q;
                ViewBag.TownId = townId;

                viewModel.SearchResults = await query
                    .OrderBy(p => p.Price)
                    .Take(200)
                    .Select(p => new SearchResultDto
                    {
                        ProductName = p.Name,
                        ChainName = p.RetailChain != null ? p.RetailChain.Name : "",
                        TownName = p.Town != null ? p.Town.Name : "",
                        Category = p.Category,
                        Price = p.Price,
                        PromoPrice = p.PromoPrice
                    })
                    .ToListAsync();


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page.");
                return View(new HomeViewModel()); 
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error()
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
