using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using SeniorProject.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace SeniorProject.Tests
{
    public class IdentitySeederTests
    {
        private IServiceProvider CreateTestServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestIdentityDB_{Guid.NewGuid()}"));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddLogging(); 

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task SeedAsync_ShouldCreateThreeRoles()
        {
            var serviceProvider = CreateTestServiceProvider();

            // call IdentitySeeder.SeedAsync with an in memory identity setup
            await IdentitySeeder.SeedAsync(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // three roles exist afterwards - Admin, RetailManager, User
            var roles = roleManager.Roles.ToList();
            Assert.Equal(3, roles.Count);
            Assert.Contains(roles, r => r.Name == "Admin");
            Assert.Contains(roles, r => r.Name == "RetailManager");
            Assert.Contains(roles, r => r.Name == "User");
        }

        [Fact]
        public async Task SeedAsync_ShouldNotCreateDuplicateRoles()
        {
            var serviceProvider = CreateTestServiceProvider();

            // call IdentitySeeder.SeedAsync twice to make sure that it would not break if the app is restarted for example
            await IdentitySeeder.SeedAsync(serviceProvider);
            await IdentitySeeder.SeedAsync(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // assert the role count is still three, not six
            var roles = roleManager.Roles.ToList();
            Assert.Equal(3, roles.Count);
        }

        [Fact]
        public async Task SeedAsync_ShouldCreateDefaultAdminAccount()
        {
            var serviceProvider = CreateTestServiceProvider();

            // after seeding
            await IdentitySeeder.SeedAsync(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // assert that a user with email admin@aubg.edu exists in the user store
            var adminUser = await userManager.FindByEmailAsync("admin@aubg.edu");
            Assert.NotNull(adminUser);
            Assert.True(await userManager.IsInRoleAsync(adminUser, "Admin"));
        }

        [Fact]
        public async Task SeedAsync_ShouldCreateDefaultManagerAccount()
        {
            var serviceProvider = CreateTestServiceProvider();

            // after seeding
            await IdentitySeeder.SeedAsync(serviceProvider);

            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // assert that a user with email manager@aubg.edu exists in the user store
            var managerUser = await userManager.FindByEmailAsync("manager@aubg.edu");
            Assert.NotNull(managerUser);
            Assert.True(await userManager.IsInRoleAsync(managerUser, "RetailManager"));
        }
    }
}
