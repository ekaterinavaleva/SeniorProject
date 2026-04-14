using Xunit;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Services;
using SeniorProject.Models;
using SeniorProject.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SeniorProject.Tests
{
    public class BasketServiceEdgeCaseTests
    {
        [Fact]
        public async Task CompareBasketAsync_ShouldReturnEmpty_WhenNoProductsMatchInTown()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // seed products in town 1
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.Towns.Add(new Town { Id = 2, Name = "Plovdiv" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });

            string productName = "мляко";
            
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = productName, ProductCode = "P1", CleanName = productName.ToCleanSortedString(),
                NameHash = productName.ToCleanSortedString().GetStableHashCode(), Category = "Dairy", 
                Price = 2.00m, RetailChainId = 1, TownId = 1, ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context);

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "мляко", Quantity = 1 }
            };

            // call CompareBasketAsync with townId 2
            var results = await service.CompareBasketAsync(basketItems, 2);

            // assert result is empty
            Assert.Empty(results);
        }

        [Fact]
        public async Task CompareBasketAsync_ShouldHandleQuantityCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // seed minimum required data
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });

            string productName = "мляко";
            
            // seed one product at price 2.00
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = productName, ProductCode = "P1", CleanName = productName.ToCleanSortedString(),
                NameHash = productName.ToCleanSortedString().GetStableHashCode(), Category = "Dairy", 
                Price = 2.00m, RetailChainId = 1, TownId = 1, ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context);

            // add it to basket with quantity 3
            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "мляко", Quantity = 3 }
            };

            var results = await service.CompareBasketAsync(basketItems, 1);

            // assert the returned total for that product is 6.00
            Assert.NotEmpty(results);
            Assert.Single(results[0].Products);
            Assert.Equal(6.00m, results[0].Products[0].Price);
            Assert.Equal(6.00m, results[0].TotalPrice);
        }

        [Fact]
        public async Task CompareBasketAsync_ShouldNotReturnMoreThanThreeStores()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // seed minimum required data
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });
            context.RetailChains.Add(new RetailChain { Id = 2, Name = "Chain B" });
            context.RetailChains.Add(new RetailChain { Id = 3, Name = "Chain C" });
            context.RetailChains.Add(new RetailChain { Id = 4, Name = "Chain D" });

            string productName = "мляко";
            int productHash = productName.ToCleanSortedString().GetStableHashCode();
            string cleanName = productName.ToCleanSortedString();
            
            // seed four retail chains each having the basket product
            for (int i = 1; i <= 4; i++)
            {
                context.ImportedProducts.Add(new ImportedProduct
                {
                    Name = productName, ProductCode = $"P1{i}", CleanName = cleanName,
                    NameHash = productHash, Category = "Dairy", 
                    Price = 2.00m, RetailChainId = i, TownId = 1, ImportDate = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            var service = new BasketService(context);

            var basketItems = new List<BasketProductDetail>
            {
                new BasketProductDetail { ProductName = "мляко", Quantity = 1 }
            };

            var results = await service.CompareBasketAsync(basketItems, 1);

            // assert results count is at most 3
            Assert.True(results.Count <= 3);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnEmpty_WhenQueryMatchesNothing()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}")
                .Options;

            using var context = new ApplicationDbContext(options);

            // seed minimum required data
            context.Towns.Add(new Town { Id = 1, Name = "Sofia" });
            context.RetailChains.Add(new RetailChain { Id = 1, Name = "Chain A" });

            // seed one product called "мляко"
            string productName = "мляко";
            context.ImportedProducts.Add(new ImportedProduct
            {
                Name = productName, ProductCode = "P1", CleanName = productName.ToCleanSortedString(),
                NameHash = productName.ToCleanSortedString().GetStableHashCode(), Category = "Dairy", 
                Price = 2.00m, RetailChainId = 1, TownId = 1, ImportDate = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new BasketService(context);

            // search for "брашно"
            var results = await service.SearchAsync("брашно", 1);

            // assert the returned list is empty
            Assert.Empty(results);
        }
    }
}
