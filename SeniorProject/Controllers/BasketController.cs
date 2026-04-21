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
            { -1, "Бял хляб от 500 гр. до 1 кг" },
            { -2, "Хляб Добруджа от 500 гр. до 1 кг" },
            { -3, "Ръжен хляб от 400 гр. до 600 гр." },
            { -4, "Типов хляб от 400 гр. до 600 гр." },
            { -5, "Точени кори от 400 гр. до 500 гр." },
            { -6, "Прясно мляко от 2 % до 3.6 % 1 л" },
            { -7, "Кисело мляко от 2 % до 3.6 % в кофички от 370 гр. до 500 гр." },
            { -8, "Сирене от краве мляко насипно 1 кг" },
            { -9, "Сирене от краве мляко пакетирано за 1 кг" },
            { -10, "Кашкавал от краве мляко насипно 1 кг" },
            { -11, "Кашкавал от краве мляко пакетирано за 1 кг" },
            { -12, "Краве масло от 125 гр. до 250 гр." },
            { -13, "Извара насипна 1 кг." },
            { -14, "Извара пакетирана от 200 гр. до 1 кг." },
            { -15, "Прясно охладено пиле 1 кг (цяло)" },
            { -16, "Пилешко филе, охладено, 1 кг" },
            { -17, "Пилешки бут, цял, охладен 1 кг" },
            { -18, "Прясно свинско месо плешка 1 кг" },
            { -19, "Прясно свинско месо бут 1 кг" },
            { -20, "Прясно свинско месо шол 1 кг" },
            { -21, "Прясно свинско месо врат 1 кг" },
            { -22, "Свинско месо за готвене 1 кг" },
            { -23, "Телешко месо шол 1 кг" },
            { -24, "Телешко месо за готвене 1 кг" },
            { -25, "Мляно месо смес 60/40, насипно за 1 кг" },
            { -26, "Кренвирши, насипни за 1 кг." },
            { -27, "Колбаси пресни от 300 гр. до 1 кг." },
            { -28, "Колбаси сухи (Шпек, Бургас, Деликатесен) от 250 гр. до 1 кг." },
            { -29, "Риба замразена (скумрия, пъстърва, лаврак, ципура) 1 кг" },
            { -30, "Риба охладена (скумрия, пъстърва, лаврак, ципура) 1 кг" },
            { -31, "Яйца размер М от 6 бр. до 10 бр. Подово отглеждане" },
            { -32, "Яйца размер L 6 бр. до 10 бр. Подово отглеждане" },
            { -33, "Боб, пакетиран 1 кг" },
            { -34, "Леща, пакетиран 1 кг" },
            { -35, "Бисерен ориз 1 кг" },
            { -36, "Макарони от 400 гр. до 500 гр." },
            { -37, "Спагети (№ 3, № 5 и № 10) 500 гр." },
            { -38, "Бяла захар 1 кг" },
            { -39, "Готварска сол 1 кг" },
            { -40, "Брашно тип 500 1 кг" },
            { -41, "Брашно екстра 1 кг" },
            { -42, "Олио слънчогледово 1 л" },
            { -43, "Зехтин 1л" },
            { -44, "Винен оцет 700 мл." },
            { -45, "Ябълков оцет 700 мл." },
            { -46, "Консерви боб, от 400 гр. до 800 гр." },
            { -47, "Консерви грах, от 400 гр. до 800 гр." },
            { -48, "Консервирани домати, от 400 гр. до 800 гр." },
            { -49, "Лютеница, от 400 гр. до 800 гр." },
            { -50, "Лимони, насипни 1кг" },
            { -51, "Портокали, насипни 1кг" },
            { -52, "Банани 1кг" },
            { -53, "Ябълки, насипни 1кг" },
            { -54, "Домати, червени, насипни 1кг" },
            { -55, "Кромид лук, насипен 1кг" },
            { -56, "Моркови, насипни 1кг" },
            { -57, "Бяло зеле 1кг" },
            { -58, "Краставици, насипни 1кг" },
            { -59, "Зрял чесън 1кг" },
            { -60, "Пресни гъби, насипни 1кг" },
            { -61, "Картофи, насипни 1кг" },
            { -62, "Маслини, насипни 1 кг" },
            { -63, "Каша (млечна, плодова) от 190 гр. до 250 гр." },
            { -64, "Детско пюре от 190 гр. до 250 гр." },
            { -65, "Адаптирани млека от 400 гр. до 800 гр." },
            { -66, "Обикновени бисквити" },
            { -67, "Кроасани от 50 гр. до 110 гр." },
            { -68, "Баница от 100 гр. до 500 гр." },
            { -69, "Шоколад, млечен, от 80 гр. до 100 гр." },
            { -70, "Кафе мляно от 200 гр. до 250 гр." },
            { -71, "Кафе на зърна 1 кг" },
            { -72, "Чай (билков на пакетчета)" },
            { -73, "Минерална вода, 6 бр. в опаковка по 1,5 л." },
            { -74, "Светла бира 2 л." },
            { -75, "Бяло вино бутилирано, произход България 750 мл." },
            { -76, "Червено вино бутилирано, произход България 750 мл." },
            { -77, "Ракия, произход България 700 мл." },
            { -78, "Тютюневи изделия, произход България, кутия, пакет" },
            { -79, "Течен препарат за миене на съдове от 400 мл." },
            { -80, "Четка за зъби – средна твърдост" },
            { -81, "Паста за зъби, туба от 50 мл. до 125 мл." },
            { -82, "Шампоан за нормална коса – от 250 мл. до 500 мл." },
            { -83, "Сапун, твърд" },
            { -84, "Класически мокри кърпи пакет" },
            { -85, "Тоалетна хартия 8 ролки" },
        };

        public async Task<IActionResult> Index()
        {
            var towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            // hide unmapped numerical codes that were saved from csv imports
            ViewBag.Towns = towns.Where(t => t.Name.Any(char.IsLetter) && t.Name != "Blagoevgrad" && t.Name != "Благоевград").ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Predefined()
        {
            // loading the towns dropdown
            var towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            // hide unmapped numerical codes that were saved directly from csv imports
            ViewBag.Towns = towns.Where(t => t.Name.Any(char.IsLetter) && t.Name != "Blagoevgrad" && t.Name != "Благоевград").ToList();
            
            // populating the categories dropdown from the product groups
            ViewBag.Categories = await _db.ProductGroups.OrderBy(g => g.Name).ToListAsync();

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

                // each result to use store address but display the chain
                foreach (var result in results)
                {
                    var chain = await _db.RetailChains
                        .FirstOrDefaultAsync(c => c.Name == result.RetailChainName);

                    if (chain != null)
                    {
                        result.StoreAddress = await _db.ImportedProducts
                            .Where(p => p.RetailChainId == chain.Id
                                     && (request.TownId == 0 || p.TownId == request.TownId)
                                     && p.StoreAddress != null)
                            .Select(p => p.StoreAddress)
                            .FirstOrDefaultAsync();
                    }
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

                var allChains = await _db.RetailChains.ToListAsync();

                RetailChain matchedChain = null;

                if (matchedChain == null)
                {
                    var chainIdFromAddress = await _db.ImportedProducts
                        .Where(p => p.StoreAddress == basket.WinningSupermarket)
                        .Select(p => (int?)p.RetailChainId)
                        .FirstOrDefaultAsync();

                    if (chainIdFromAddress.HasValue)
                    {
                        matchedChain = allChains.FirstOrDefault(c => c.Id == chainIdFromAddress.Value);
                    }
                }

                // if no chain could be matched return a clear response 
                if (matchedChain == null)
                {
                    return Json(new
                    {
                        savedDate = basket.SavedDate.ToString("MMM dd, yyyy"),
                        totalSavedPrice = basket.TotalPrice,
                        totalCurrentPrice = 0m,
                        chainNotFound = true
                    });
                }

                // filter products by the resolved chain id and town
                var query = _db.ImportedProducts.Where(p => p.RetailChainId == matchedChain.Id);
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
                            .Where(p => p.NameHash == nameHash)
                            .OrderBy(p => p.PromoPrice ?? p.Price)
                            .FirstOrDefault();

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
        [HttpPost]
        public async Task<IActionResult> DeleteBasket(int basketId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var basket = await _db.SavedBaskets
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == basketId && b.UserId == userId);

            if (basket == null) return NotFound();

            _db.SavedBaskets.Remove(basket);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MyBaskets));
        }
    }
}
