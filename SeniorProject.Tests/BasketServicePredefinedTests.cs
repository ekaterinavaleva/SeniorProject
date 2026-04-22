using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SeniorProject.Data;
using SeniorProject.Services;
using SeniorProject.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SeniorProject.Tests
{
    public class BasketServicePredefinedTests
    {
        [Fact]
        public async Task ComparePredefinedBasketAsync_ShouldMatchByCategory_WhenNegativeIdProvided()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });

            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = "Прясно мляко 1л",
                ProductCode = "ML001",
                CleanName = "Прясно мляко 1л",
                NameHash = 0,
                Category = "6",
                Price = 1.50m,
                PromoPrice = null,
                TownId = 1,
                RetailChainId = 1,
                ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context, new MemoryCache(new MemoryCacheOptions()));

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "Прясно мляко", Quantity = 1, CategoryId = -6 }
            };

            var results = await service.ComparePredefinedBasketAsync(basketItems, 1);

            Assert.NotEmpty(results);
            Assert.Single(results[0].Products);
            Assert.Equal(1.50m, results[0].TotalPrice);
        }

        [Fact]
        public async Task ComparePredefinedBasketAsync_ShouldReturnEmpty_WhenNoCategoryMatchExists()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });

            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = "Кисело мляко 400г",
                ProductCode = "KM001",
                CleanName = "Кисело мляко 400г",
                NameHash = 0,
                Category = "7",
                Price = 1.20m,
                PromoPrice = null,
                TownId = 1,
                RetailChainId = 1,
                ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context, new MemoryCache(new MemoryCacheOptions()));

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "Прясно мляко", Quantity = 1, CategoryId = -6 }
            };

            var results = await service.ComparePredefinedBasketAsync(basketItems, 1);

            var hasNoMatch = results.Count == 0 || results[0].Products.Count == 0;
            Assert.True(hasNoMatch);
        }
    }
}
