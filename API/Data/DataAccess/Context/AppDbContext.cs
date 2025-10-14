using System.Linq.Expressions;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DATA.DataAccess.Context
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets for all entities
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ReturnItem> ReturnItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all configurations from the assembly
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            ApplyGlobalSoftDeleteFilters(builder);
        }

        private static void ApplyGlobalSoftDeleteFilters(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Skip owned/entity types (e.g., Product.Size)
                if (entityType.IsOwned())
                    continue;

                var clrType = entityType.ClrType;

                // Only apply to types that have a public bool IsDeleted property
                var isDeletedProp = clrType.GetProperty("IsDeleted");
                if (isDeletedProp == null || isDeletedProp.PropertyType != typeof(bool))
                    continue;

                var parameter = Expression.Parameter(clrType, "e");
                var prop = Expression.Property(parameter, isDeletedProp);
                var body = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(body, parameter);

                builder.Entity(clrType).HasQueryFilter(lambda);
            }

        }
    }
}
