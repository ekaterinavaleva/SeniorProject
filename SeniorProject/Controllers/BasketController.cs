using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Services;

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
        public async Task<IActionResult> SearchProducts(string term)
        {
            var results = await _basketService.SearchAsync(term);
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

        public class CompareRequest
        {
            public List<string> Items { get; set; }
            public int TownId { get; set; }
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
    }
}
