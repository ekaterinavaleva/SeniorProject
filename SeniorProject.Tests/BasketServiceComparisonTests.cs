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
    public class BasketServiceComparisonTests
    {
        [Fact]
        public async Task CompareBasketAsync_ShouldRankChainWithMoreMatchesFirst_EvenIfCheaper()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // minimum required data including towns and chains
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A (Complete)" });
            context.RetailChains.Add(new RetailChain { Id = 2, Name = "Chain B (Cheaper but incomplete)" });

            var importDate = DateTime.UtcNow;

            string product1Name = "Milk 1L";
            int product1Hash = product1Name.ToCleanSortedString().GetStableHashCode();

            string product2Name = "Bread 500g";
            int product2Hash = product2Name.ToCleanSortedString().GetStableHashCode();

            //  both products to chain A with standard prices (total 5.00)
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = product1Name, ProductCode = "P1A", CleanName = product1Name.ToCleanSortedString(),
                NameHash = product1Hash, Category = "Dairy", Price = 2.00m, RetailChainId = 1, TownId = 1, ImportDate = importDate
            });
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = product2Name, ProductCode = "P2A", CleanName = product2Name.ToCleanSortedString(),
                NameHash = product2Hash, Category = "Bakery", Price = 3.00m, RetailChainId = 1, TownId = 1, ImportDate = importDate
            });

            //  one product to chain B with a very cheap price (total 0.50)
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = product1Name, ProductCode = "P1B", CleanName = product1Name.ToCleanSortedString(),
                NameHash = product1Hash, Category = "Dairy", Price = 0.50m, RetailChainId = 2, TownId = 1, ImportDate = importDate
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context, new MemoryCache(new MemoryCacheOptions()));

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "Milk 1L", Quantity = 1 },
                new BasketProductDetail { ProductName = "Bread 500g", Quantity = 1 }
            };

            var results = await service.CompareBasketAsync(basketItems, 1);

            // verifying the results order prioritizes match count over partial total price
            Assert.NotEmpty(results);
            Assert.Equal(2, results.Count);

            Assert.Equal("Chain A (Complete)", results[0].RetailChainName);
            Assert.Equal(2, results[0].Products.Count);
            
            Assert.Equal("Chain B (Cheaper but incomplete)", results[1].RetailChainName);
            Assert.Equal(1, results[1].Products.Count);
        }
    }
}
