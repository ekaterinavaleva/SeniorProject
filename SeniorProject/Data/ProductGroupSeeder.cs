using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;

namespace SeniorProject.Data
{
    public static class ProductGroupSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //  subcategories that the retail manager can map for the user's small basket
            string[] exactCategories = new[]
            {
                "Хляб \"Добруджа\" (600-650 гр.)",
                "Ориз, български бял (1 кг.)",
                "Свински бут, без кост (1 кг.)",
                "Кренвирш, свински, насипни (1 кг.)",
                "Прясно мляко, 3% (1 л.)",
                "Кисело мляко, 400 гр. (3,6%)",
                "Яйца, кокоши, М размер (10 броя)",
                "Бял боб, пакетиран (1 кг.)"
            };

            // get whatever groups already exist in the database table
            var existingGroups = await db.ProductGroups.ToListAsync();

            bool changed = false;

            // if any category is missing from the database, they get added
            foreach (var category in exactCategories)
            {
                if (!existingGroups.Any(g => g.Name == category))
                {
                    db.ProductGroups.Add(new ProductGroup { Name = category });
                    changed = true;
                }
            }

            // if missing categories were found and added, save the changes to the database
            if (changed)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
