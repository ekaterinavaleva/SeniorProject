using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Controllers
{
    [Authorize(Roles = "Admin, RetailManager")]
    public class RetailManagerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RetailManagerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile zipFile)
        {
            if (zipFile == null)
            {
                return View();
            }

            string root = Directory.GetCurrentDirectory();
            string uploadFolder = Path.Combine(root, "uploads");

            Directory.CreateDirectory(uploadFolder);

            string zipPath = Path.Combine(uploadFolder, "latest_upload.zip");

            using (var stream = new FileStream(zipPath, FileMode.Create))
            {
                await zipFile.CopyToAsync(stream);
            }

            string extractPath = Path.Combine(uploadFolder, "extracted");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);

            var encoding = System.Text.Encoding.UTF8;
            _db.ChangeTracker.AutoDetectChangesEnabled = false;
            _db.Database.SetCommandTimeout(300);

            try
            {
                var categoryMap = new Dictionary<string, string>
                {
                    { "9", "Milk" },
                    { "24", "Beef" },
                    { "20", "Pork" }
                };

                var townMap = new Dictionary<string, string>
                {
                    { "68134", "Sofia" },
                    { "56784", "Plovdiv" },
                    { "10135", "Varna" }
                };

                var existingTowns = await _db.Towns.ToDictionaryAsync(t => t.Name, t => t.Id);
                var existingChains = await _db.RetailChains.ToDictionaryAsync(c => c.Name, c => c.Id);
                var newProducts = new List<ImportedProduct>();

                foreach (var file in Directory.GetFiles(extractPath, "*.csv", SearchOption.AllDirectories))
                {
                    using (var reader = new StreamReader(file, encoding))
                    {
                        reader.ReadLine();

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();

                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var cols = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                            if (cols.Length < 7) continue;

                            string townCode = cols[0].Trim('"');
                            string productName = cols[2];
                            string categoryCode = cols[4];
                            string priceText = cols[5];
                            string promoText = cols[6].Trim('"');

                            string townName = townCode;
                            if (townMap.ContainsKey(townCode))
                            {
                                townName = townMap[townCode];
                            }

                            string category = categoryCode;
                            if (categoryMap.ContainsKey(categoryCode))
                            {
                                category = categoryMap[categoryCode];
                            }

                            if (!decimal.TryParse(priceText, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(promoText))
                            {
                                if (decimal.TryParse(promoText, System.Globalization.CultureInfo.InvariantCulture, out decimal promo) && promo > 0)
                                {
                                    price = promo;
                                }
                            }

                            if (!existingTowns.ContainsKey(townName))
                            {
                                var newTown = new Town { Name = townName };
                                _db.Towns.Add(newTown);
                                await _db.SaveChangesAsync();
                                existingTowns[townName] = newTown.Id;
                            }
                            int townId = existingTowns[townName];

                            string chainName = cols[1];
                            if (!existingChains.ContainsKey(chainName))
                            {
                                var newChain = new RetailChain { Name = chainName };
                                _db.RetailChains.Add(newChain);
                                await _db.SaveChangesAsync();
                                existingChains[chainName] = newChain.Id;
                            }
                            int chainId = existingChains[chainName];

                            var product = new ImportedProduct
                            {
                                Name = productName,
                                ProductCode = cols[3],
                                Category = category,
                                Price = price,
                                TownId = townId,
                                RetailChainId = chainId,
                                ImportDate = DateTime.UtcNow
                            };

                            newProducts.Add(product);

                            if (newProducts.Count >= 1000)
                            {
                                await _db.ImportedProducts.AddRangeAsync(newProducts);
                                await _db.SaveChangesAsync();
                                newProducts.Clear();
                            }
                        }
                    }
                }

                if (newProducts.Any())
                {
                    await _db.ImportedProducts.AddRangeAsync(newProducts);
                    await _db.SaveChangesAsync();
                }
            }
            finally
            {
                _db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            ViewBag.Message = "Upload successful!";
            return View();
        }
    }
}
