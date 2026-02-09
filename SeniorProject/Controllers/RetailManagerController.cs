using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
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

            var encoding = System.Text.Encoding.GetEncoding(1251);

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

                        decimal price = decimal.Parse(priceText, System.Globalization.CultureInfo.InvariantCulture);

                        if (!string.IsNullOrEmpty(promoText))
                        {
                            decimal promo = decimal.Parse(promoText, System.Globalization.CultureInfo.InvariantCulture);
                            if (promo > 0)
                            {
                                price = promo;
                            }
                        }

                        var town = _db.Towns.FirstOrDefault(t => t.Name == townName);
                        if (town == null)
                        {
                            town = new Town { Name = townName };
                            _db.Towns.Add(town);
                            _db.SaveChanges();
                        }

                        string chainName = cols[1];
                        var chain = _db.RetailChains.FirstOrDefault(c => c.Name == chainName);
                        if (chain == null)
                        {
                            chain = new RetailChain { Name = chainName };
                            _db.RetailChains.Add(chain);
                            _db.SaveChanges();
                        }

                        var product = new ImportedProduct
                        {
                            Name = productName,
                            Category = category,
                            Price = price,
                            TownId = town.Id,
                            RetailChainId = chain.Id,
                            ImportDate = DateTime.UtcNow
                        };

                        _db.ImportedProducts.Add(product);
                        _db.SaveChanges();
                    }
                }
            }

            ViewBag.Message = "Upload successful!";
            return View();
        }
    }
}
