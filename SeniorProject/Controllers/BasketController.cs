using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Services;
using SeniorProject.Models;
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
            ViewBag.Towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term, int? townId)
        {
            var results = await _basketService.SearchAsync(term, townId);
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
                var results = await _basketService.CompareBasketAsync(request.Items, request.TownId);
                return Json(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugChains(int townId)
        {
            if (townId == 0) return BadRequest("Provide townId");

            var data = await _db.ImportedProducts
                .Where(p => p.TownId == townId)
                .GroupBy(p => p.RetailChain.Name)
                .Select(g => new { 
                    Chain = g.Key, 
                    ProductCount = g.Count(), 
                    LatestUpload = g.Max(p => p.ImportDate),
                    SampleProduct = g.Select(p => p.Name).FirstOrDefault()
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBasket([FromBody] SaveBasketRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("You must be logged in to save a basket.");
            }

            // Condense all data validation into one clean check
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
    }
}
