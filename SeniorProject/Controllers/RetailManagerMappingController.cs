using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Controllers
{
    public class RetailManagerMappingController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index(int? groupId, string? q)
        {
            // get the Groups for the dropdown (Bread, Milk, etc.)
            ViewBag.Groups = await db.ProductGroups.OrderBy(g => g.Name).ToListAsync();
            ViewBag.GroupId = groupId;
            ViewBag.Query = q;

            var productNames = new List<string>();

            // only scan files if the user has selected a group
            if (groupId != null)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "extracted");
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.GetFiles(path, "*.csv",SearchOption.AllDirectories))
                    {
                        using var reader = new StreamReader(file);
                        reader.ReadLine(); // skip header

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var cols = line.Split(',');
                            if (cols.Length > 2)
                            {
                                // column index 2 is the product name in the files
                                string name = cols[2].Replace("\"", "").Trim();

                                // directly add to list without filtering
                                productNames.Add(name);
                            }
                        }
                    }
                }
            }

            return View(productNames);
        }


    }
}
