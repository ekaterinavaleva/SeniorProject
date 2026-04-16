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
        public DbSet<SavedBasket> SavedBaskets { get; set; } = default!;
        public DbSet<SavedBasketItem> SavedBasketItems { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ImportedProduct>()
                .HasIndex(p => new { p.TownId, p.RetailChainId, p.ImportDate });
            
            builder.Entity<ImportedProduct>()
                .HasIndex(p => p.Name);

            builder.Entity<ImportedProduct>()
                .HasIndex(p => p.CleanName);
        }
    }
}
