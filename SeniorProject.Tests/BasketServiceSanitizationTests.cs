using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SeniorProject.Data;
using SeniorProject.Services;
using SeniorProject.Models;
using SeniorProject.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SeniorProject.Tests
{
    public class BasketServiceSanitizationTests
    {
        [Theory]
        [InlineData("Брашно Тип 500")]
        [InlineData("брашно тип 500.")]
        [InlineData("БРАШНО ТИП 500")]
        [InlineData("Брашно-тип.500")]
        [InlineData("Брашно, тип 500")]
        [InlineData("=*Брашно, ТИП-500*=")]

        public async Task CompareBasketAsync_ShouldMatchProduct_RegardlessOfSanitization(string inputBasketName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;
                
            using var context = new ApplicationDbContext(options);

            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Kaufland" });

            string dbOriginalName = "Брашно Тип 500";
            string cleanName = dbOriginalName.ToCleanSortedString();
            int nameHash = cleanName.GetStableHashCode();

            context.ImportedProducts.Add(new ImportedProduct 
            { 
                Name = dbOriginalName, 
                ProductCode = "BR500",
                CleanName = cleanName,
                NameHash = nameHash,
                Category = "Staples",
                Price = 1.50m, 
                RetailChainId = 1, 
                TownId = 1, 
                ImportDate = DateTime.UtcNow 
            });
            await context.SaveChangesAsync();

            var service = new BasketService(context, new MemoryCache(new MemoryCacheOptions()));
            
            var basketItems = new List<BasketProductDetail> 
            { 
                new BasketProductDetail { ProductName = inputBasketName, Quantity = 1 } 
            };

            var results = await service.CompareBasketAsync(basketItems, 1);

            Assert.NotEmpty(results);
            Assert.Single(results[0].Products);
            Assert.Equal(dbOriginalName, results[0].Products[0].ProductName);
        }

        [Fact]
        public async Task CompareBasketAsync_ShouldPrioritizePromoPrice_WhenAvailable()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "MegaMarket" });

            string dbOriginalName = "Milk 1L";
            string cleanName = dbOriginalName.ToCleanSortedString();
            int nameHash = cleanName.GetStableHashCode();

            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = dbOriginalName,
                ProductCode = "M1-A",
                CleanName = cleanName,
                NameHash = nameHash,
                Category = "Dairy",
                Price = 1.50m,
                PromoPrice = null,
                RetailChainId = 1,
                TownId = 1,
                ImportDate = DateTime.UtcNow
            });

            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = dbOriginalName,
                ProductCode = "M1-B",
                CleanName = cleanName,
                NameHash = nameHash,
                Category = "Dairy",
                Price = 1.80m,
                PromoPrice = 1.20m,
                RetailChainId = 1,
                TownId = 1,
                ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context, new MemoryCache(new MemoryCacheOptions()));

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "Milk 1L", Quantity = 1 }
            };

            var results = await service.CompareBasketAsync(basketItems, 1);

            Assert.NotEmpty(results);
            Assert.Single(results[0].Products);
            
            var selectedProduct = results[0].Products[0];
            Assert.Equal(dbOriginalName, selectedProduct.ProductName);
            Assert.Equal(1.20m, selectedProduct.Price); 
            Assert.True(selectedProduct.IsPromo);
        }
    }
}
