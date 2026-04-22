using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Controllers
{
    [Authorize(Roles = "Admin, RetailManager")]
    public class RetailManagerMappingController(ApplicationDbContext db, IMemoryCache cache) : Controller
    {
        // cache latest date so it doesn't requery on every page load
        private static DateTime? _cachedLatestDate;

        public async Task<IActionResult> Index(int? groupId, string? q, int? townId)
        {
            // load groups with their current mapped product count
            ViewBag.Groups = await db.ProductGroups
                .AsNoTracking()
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    Count = db.ProductGroupItems.Count(i => i.ProductGroupId == g.Id)
                })
                .OrderBy(g => g.Name)
                .ToListAsync();

            // load towns for the filter dropdown
            var towns = await db.Towns.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
            // hide unmapped numerical codes that were saved from csv imports
            ViewBag.Towns = towns.Where(t => t.Name.Any(char.IsLetter) && t.Name != "Blagoevgrad" && t.Name != "Благоевград").ToList();

            // preserve filter state across requests
            ViewBag.GroupId = groupId;
            ViewBag.Query = q;
            ViewBag.TownId = townId;

            var products = new List<MappedProductViewModel>();

            if (groupId != null)
            {
                var cacheKey = townId.HasValue ? $"MappingCache_Town_{townId.Value}" : "GlobalMappingCache";
                var allProducts = await cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48);

                    _cachedLatestDate ??= await db.ImportedProducts.MaxAsync(p => (DateTime?)p.ImportDate);

                    var queryDb = db.ImportedProducts.AsNoTracking();
                    if (_cachedLatestDate.HasValue)
                        queryDb = queryDb.Where(p => p.ImportDate == _cachedLatestDate.Value);

                    if (townId.HasValue)
                        queryDb = queryDb.Where(p => p.TownId == townId.Value);

                    var dbProducts = await queryDb
                        .Select(p => new { p.NameHash, p.Name })
                        .Distinct()
                        .ToListAsync();

                    return dbProducts.Select(p => (p.NameHash, p.Name)).ToList();
                });

                if (allProducts == null) allProducts = new List<(int, string)>();

                var filteredProducts = allProducts.AsEnumerable();

                if (!string.IsNullOrEmpty(q))
                {
                    var searchWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in searchWords)
                    {
                        filteredProducts = filteredProducts.Where(p => p.Name != null && p.Name.Contains(word, StringComparison.OrdinalIgnoreCase));
                    }
                }

                // distinct products by hash, max 200 to keep the page fast
                var distinctProducts = filteredProducts
                    .OrderBy(p => p.Name)
                    .Take(200)
                    .ToList();

                // load existing mappings for the selected group
                var mappedItems = await db.ProductGroupItems
                    .Where(i => i.ProductGroupId == groupId.Value)
                    .ToListAsync();

                foreach (var product in distinctProducts)
                {
                    var map = mappedItems.FirstOrDefault(m => m.RawProductId == product.NameHash);
                    products.Add(new MappedProductViewModel
                    {
                        NameHash = product.NameHash,
                        Name = product.Name,
                        MappingId = map?.Id
                    });
                }
            }

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> AddToGroup([FromForm] int nameHash, [FromForm] string productName, [FromForm] int groupId)
        {
            // avoid duplicate mappings
            var existing = await db.ProductGroupItems
                .FirstOrDefaultAsync(m => m.RawProductId == nameHash && m.ProductGroupId == groupId);

            if (existing != null)
                return Json(new { mappingId = existing.Id });

            // store the namehash in rawproductid as the matching key
            var item = new ProductGroupItem
            {
                ProductGroupId = groupId,
                RawProductId = nameHash,
                MappedName = productName
            };
            db.ProductGroupItems.Add(item);
            await db.SaveChangesAsync();

            return Json(new { mappingId = item.Id });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromGroup([FromForm] int mappingId)
        {
            var item = await db.ProductGroupItems.FindAsync(mappingId);

            if (item != null)
            {
                db.ProductGroupItems.Remove(item);
                await db.SaveChangesAsync();
            }

            return Json(new { ok = true });
        }
    }

    public class MappedProductViewModel
    {
        public int NameHash { get; set; }
        public string Name { get; set; }
        public int? MappingId { get; set; }
        public bool IsMapped => MappingId.HasValue;
    }
}