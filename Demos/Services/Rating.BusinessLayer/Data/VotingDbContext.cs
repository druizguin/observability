namespace Rating.BusinessLayer.Data;

using Microsoft.EntityFrameworkCore;
using Rating.BusinessLayer.Dom;

public class VotingDbContext : DbContext
{
    public VotingDbContext(DbContextOptions<VotingDbContext> options) : base(options) { }

    public DbSet<Dom.Vote> Votes => Set<Dom.Vote>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductValidation> ProductValidations => Set<ProductValidation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasMany(p => p.Votes)
            .WithOne(p=>p.Producto)
            .HasForeignKey(v => v.ProductoId);

        modelBuilder.Entity<Product>()
           .HasMany(p => p.Validations)
           .WithOne()
           .HasForeignKey(v => v.ProductoId);
    }
}