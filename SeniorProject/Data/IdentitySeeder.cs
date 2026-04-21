using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SeniorProject.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using (var scope = services.CreateScope()) // this is to access the services
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); // to access the service provider

                var roles = new[] { "Admin", "RetailManager", "User" }; // define the roles 

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role)) // check if the role exists
                    {
                        await roleManager.CreateAsync(new IdentityRole(role)); // create the role if it does not exist
                    }
                }
            }//every time the application starts, it will check and create roles if they do not exist

            using (var scope = services.CreateScope()) 
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(); 

                string email = "admin@aubg.edu";
                string password = "SPAdmin8*";

                if (await userManager.FindByEmailAsync(email) == null) //check if account exists
                {
                    //create an account
                    var user = new IdentityUser();
                    user.Email = email;
                    user.UserName = email;

                    await userManager.CreateAsync(user, password);

                    // after creating it,we add it to the admin role
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            using (var scope = services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                string email = "manager@aubg.edu";
                string password = "SPManager8*";

                if (await userManager.FindByEmailAsync(email) == null) //check if account exists
                {
                    //create an account
                    var user = new IdentityUser();
                    user.Email = email;
                    user.UserName = email;

                    await userManager.CreateAsync(user, password);

                    // after creating it, we add it to the manager role
                    await userManager.AddToRoleAsync(user, "RetailManager");
                }
            }

            // assign the user role to any registered user who has no role yet
            using (var scope = services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                foreach (var user in await userManager.Users.ToListAsync())
                {
                    var roles = await userManager.GetRolesAsync(user);
                    if (roles.Count == 0)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }
        }
        }
    }


