using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Extensions;
using SeniorProject.Models;
using SeniorProject.Services;
using System.Security.Claims;

namespace SeniorProject.Controllers
{
    public class BasketController : Controller
    {
        private readonly BasketService _basketService;
        private readonly ApplicationDbContext _db;

        public BasketController(BasketService basketService, ApplicationDbContext db)
        {
            _basketService = basketService;
            _db = db;
        }

        private static readonly Dictionary<int, string> PredefinedCategories = new Dictionary<int, string>
        {
            { -1, "Хляб бял, нарязан" },
            { -2, "Хляб Добруджа" },
            { -3, "Хляб ръжен/тъмен" },
            { -4, "Хляб типов, нарязан" },
            { -5, "Точени кори / фило" },
            { -6, "Прясно мляко" },
            { -7, "Кисело мляко 400г" },
            { -8, "Бяло саламурено сирене, насипно" },
            { -9, "Бяло саламурено сирене, пакетирано" },
            { -10, "Кашкавал, насипно" },
            { -11, "Кашкавал, пакетиран" },
            { -12, "Краве масло" },
            { -13, "Извара, насипна" },
            { -14, "Извара, пакетирана" },
            { -15, "Цяло пиле, охладено" },
            { -16, "Пилешко филе" },
            { -17, "Пилешко бутче/бут" },
            { -18, "Свинска плешка, без кост" },
            { -19, "Свински бут, без кост" },
            { -21, "Свински врат, без кост" },
            { -22, "Свинско месо за готвене" },
            { -26, "Кренвирши, насипни" },
            { -27, "Колбас" },
            { -28, "Салам" },
            { -30, "Риба (скумрия)" },
            { -31, "Яйца M размер, 10 броя" },
            { -32, "Яйца L размер, 10 броя" },
            { -35, "Ориз" },
            { -36, "Макарони" },
            { -37, "Спагети" },
            { -38, "Захар" },
            { -39, "Сол" },
            { -40, "Брашно тип 500" },
            { -41, "Брашно екстра" },
            { -42, "Олио слънчогледово" },
            { -43, "Зехтин" },
            { -44, "Винен оцет" },
            { -45, "Ябълков оцет" },
            { -47, "Грах, консервиран" },
            { -48, "Домати, белени консервирани" },
            { -49, "Лютеница" },
            { -50, "Лимони" },
            { -51, "Портокали" },
            { -52, "Банани" },
            { -53, "Ябълки" },
            { -54, "Червени домати, пресни" },
            { -55, "Кромид лук" },
            { -56, "Моркови" },
            { -57, "Зеле" },
            { -58, "Краставици" },
            { -61, "Картофи" },
            { -62, "Маслини" },
            { -63, "Бебешка каша" },
            { -64, "Бебешко пюре" },
            { -65, "Адаптирано мляко за кърмачета" },
            { -66, "Бисквити" },
            { -67, "Кроасан" },
            { -68, "Баница/пура със сирене" },
            { -69, "Шоколад" },
            { -70, "Кафе смляно" },
            { -71, "Кафе на зърна" },
            { -72, "Чай" },
            { -73, "Минерална вода" },
            { -74, "Бира" },
            { -75, "Бяло вино" },
            { -76, "Червено вино" },
            { -77, "Ракия / спиртни напитки" }
        };

        public async Task<IActionResult> Index()
        {
            var towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            // hide unmapped numerical codes that were saved from csv imports
            ViewBag.Towns = towns.Where(t => t.Name.Any(char.IsLetter)).ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Predefined()
        {
            // loading the towns dropdown
            var towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            // hide unmapped numerical codes that were saved directly from csv imports
            ViewBag.Towns = towns.Where(t => t.Name.Any(char.IsLetter)).ToList();
            
            // populating the categories dropdown from the product groups
            ViewBag.Categories = await _db.ProductGroups.OrderBy(g => g.Name).ToListAsync();
            
            // explicitly mapped unmapped categories
            ViewBag.UnmappedCategories = PredefinedCategories;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term, int? townId, CancellationToken cancellationToken)
        {
            var results = await _basketService.SearchAsync(term, townId, cancellationToken);
            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> CompareBasket([FromBody] CompareRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return BadRequest("No items in basket.");
            }

            try
            {
                List<BasketComparisonResult> results;
                if (request.IsPredefined)
                {
                    // comparing the predefined category basket using the mapping logic
                    results = await _basketService.ComparePredefinedBasketAsync(request.Items, request.TownId);
                }
                else
                {
                    // comparing the custom basket using string search
                    results = await _basketService.CompareBasketAsync(request.Items, request.TownId);
                }
                return Json(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveBasket([FromBody] SaveBasketRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("You must be logged in to save a basket.");
            }

            //  all data validation into one check
            if (request == null || string.IsNullOrEmpty(request.WinningSupermarket) || request.Items == null || !request.Items.Any())
            {
                return BadRequest("Invalid or missing basket data.");
            }

            var savedBasket = new SavedBasket
            {
                UserId = userId,
                SavedDate = DateTime.UtcNow,
                WinningSupermarket = request.WinningSupermarket,
                TotalPrice = request.TotalPrice,
                TownId = request.TownId,
                Items = request.Items.Select(i => new SavedBasketItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price / (i.Quantity > 0 ? i.Quantity : 1) // Store unit price for reference
                }).ToList()
            };

            _db.SavedBaskets.Add(savedBasket);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Basket saved successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> MyBaskets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var baskets = await _db.SavedBaskets
                .Include(b => b.Items)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.SavedDate)
                .ToListAsync();

            return View(baskets);
        }

        [HttpGet]
        public async Task<IActionResult> GetBasketPriceComparison(int basketId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var basket = await _db.SavedBaskets
                    .Include(b => b.Items)
                    .FirstOrDefaultAsync(b => b.Id == basketId && b.UserId == userId);

                if (basket == null) return NotFound("Basket not found");

                // find the most recent import date for the specific supermarket
                var query = _db.ImportedProducts.Where(p => p.RetailChain.Name == basket.WinningSupermarket);
                if (basket.TownId.HasValue) 
                {
                    query = query.Where(p => p.TownId == basket.TownId.Value);
                }

                var latestImportDate = await query.MaxAsync(p => (DateTime?)p.ImportDate);

                decimal totalCurrentPrice = 0;
                var currentProducts = await query
                    .Where(p => p.ImportDate == latestImportDate)
                    .ToListAsync();

                foreach (var item in basket.Items)
                {
                    if (latestImportDate.HasValue)
                    {
                        var cleanName = item.ProductName.ToCleanSortedString();
                        var nameHash = cleanName.GetStableHashCode();

                        var currentProduct = currentProducts
                            .FirstOrDefault(p => p.NameHash == nameHash);

                        if (currentProduct != null)
                        {
                            var itemCurrentPrice = currentProduct.PromoPrice ?? currentProduct.Price;
                            totalCurrentPrice += itemCurrentPrice * item.Quantity;
                        }
                    }
                }

                var result = new 
                {
                    savedDate = basket.SavedDate.ToString("MMM dd, yyyy"),
                    totalSavedPrice = basket.TotalPrice,
                    totalCurrentPrice = totalCurrentPrice
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace, inner = ex.InnerException?.Message });
            }
        }
    }
}
