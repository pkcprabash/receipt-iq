using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ReceiptIqDbContext(DbContextOptions<ReceiptIqDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLineItem> ReceiptLineItems => Set<ReceiptLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasIndex(m => m.Name);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<CategoryRule>(entity =>
        {
            entity.HasOne(cr => cr.Category).WithMany().HasForeignKey(cr => cr.CategoryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cr => cr.Merchant).WithMany().HasForeignKey(cr => cr.MerchantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cr => cr.User).WithMany().HasForeignKey(cr => cr.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.Property(r => r.TotalAmount).HasPrecision(18, 2);
            entity.Property(r => r.RawOcrResponse).HasColumnType("jsonb");
            entity.Ignore(r => r.DominantCategoryId);
            entity.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Merchant).WithMany().HasForeignKey(r => r.MerchantId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReceiptLineItem>(entity =>
        {
            entity.Property(li => li.Amount).HasPrecision(18, 2);
            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
            entity.Property(li => li.Quantity).HasPrecision(18, 3);
            entity.HasOne(li => li.Receipt).WithMany(r => r.LineItems).HasForeignKey(li => li.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(li => li.Category).WithMany().HasForeignKey(li => li.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
