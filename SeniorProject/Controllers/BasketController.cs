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
                var latestImportDate = await _db.ImportedProducts
                    .Where(p => p.RetailChain.Name == basket.WinningSupermarket)
                    .MaxAsync(p => (DateTime?)p.ImportDate);

                decimal totalCurrentPrice = 0;
                var currentProducts = await _db.ImportedProducts
                    .Where(p => p.RetailChain.Name == basket.WinningSupermarket &&
                       p.ImportDate == latestImportDate)
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
