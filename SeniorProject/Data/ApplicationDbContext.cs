using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Models;

namespace SeniorProject.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Town> Towns { get; set; }
        public DbSet<RetailChain> RetailChains { get; set; }
        public DbSet<ProductGroup> ProductGroups { get; set; } = default!;
        public DbSet<ProductGroupItem> ProductGroupItems { get; set; } = default!;
        public DbSet<ImportedProduct> ImportedProducts { get; set; } = default!;

    }
}
