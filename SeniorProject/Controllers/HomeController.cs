using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Authorization;
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

        public async Task<IActionResult> Index(string? q, int? townId, string? category)
        {
            var query = _db.ImportedProducts
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

            ViewBag.Categories = await _db.ImportedProducts
                                          .Select(p => p.Category)
                                          .Distinct()
                                          .OrderBy(c => c)
                                          .ToListAsync();

            ViewBag.Towns = await _db.Towns.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Query = q;
            ViewBag.TownId = townId;

            var results = await query.OrderBy(p => p.Price).Take(200).ToListAsync();

            return View(results);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
