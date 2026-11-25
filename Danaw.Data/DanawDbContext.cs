using Danaw.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Danaw.Data;

public class DanawDbContext : DbContext
{
    public DanawDbContext(DbContextOptions<DanawDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Reviewer> Reviewers => Set<Reviewer>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentItemSource> ContentItemSources => Set<ContentItemSource>();
    public DbSet<ValidationRecord> ValidationRecords => Set<ValidationRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Category>(e =>
        {
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Name).IsRequired().HasMaxLength(60);
            e.Property(x => x.Description).HasMaxLength(255);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(
                new Category { CategoryId = 1, Name = "Arts/Crafts", Description = "Traditional arts, crafts and motifs" },
                new Category { CategoryId = 2, Name = "Heritage", Description = "Heritage practices and textiles" },
                new Category { CategoryId = 3, Name = "History", Description = "Historical accounts and epics" },
                new Category { CategoryId = 4, Name = "Places", Description = "Cultural sites and places" });
        });

        mb.Entity<Reviewer>(e =>
        {
            e.HasKey(x => x.ReviewerId);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.Username).IsRequired().HasMaxLength(50).UseCollation("NOCASE");
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Username).IsUnique();

            // DEMO accounts only. Passwords are stored as salted PBKDF2 hashes, never as plain text.
            // The demo passwords are listed in the README and are for demonstration and testing.
            e.HasData(
                new Reviewer
                {
                    ReviewerId = 1,
                    FullName = "Balindong, S. H.",
                    Role = ReviewerRole.Researcher,
                    IsActive = true,
                    Username = "balindong",
                    PasswordHash = "PBKDF2-SHA256$600000$Yx8fqvAhckUhuguzM3+1cA==$LZeoBUy9ho89yRHc46sBAUMgky/LsaNlu16kTkPTGXY="
                },
                new Reviewer
                {
                    ReviewerId = 2,
                    FullName = "Balofinos, F. S.",
                    Role = ReviewerRole.Researcher,
                    IsActive = true,
                    Username = "balofinos",
                    PasswordHash = "PBKDF2-SHA256$600000$yUs4zzD7MRVOlTWQZkU9zQ==$yfjn/zhez6erRallXMzkNUvIcgPwoKyJ9uJIpG2evNc="
                },
                new Reviewer
                {
                    ReviewerId = 3,
                    FullName = "Cabasag, E. G.",
                    Role = ReviewerRole.Researcher,
                    IsActive = true,
                    Username = "cabasag",
                    PasswordHash = "PBKDF2-SHA256$600000$cpGMZ/PJJqFTBGXQo34q5A==$Fww7lMadnYKsmivaFt+iG1M+Aiv2+Mag72rlvRMaTDw="
                },
                new Reviewer
                {
                    ReviewerId = 4,
                    FullName = "BCPCH Validator",
                    Role = ReviewerRole.Validator,
                    IsActive = true,
                    Username = "validator",
                    PasswordHash = "PBKDF2-SHA256$600000$PvhdP/mHm28+UeqD50XUpA==$fWUTiC8aX92dNXeJPHN7aYh2QMTnad+FuLj2jl0jIUk="
                });
        });

        mb.Entity<Source>(e =>
        {
            e.HasKey(x => x.SourceId);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.SourceType).HasMaxLength(30);
            e.Property(x => x.Publisher).HasMaxLength(100);
            e.Property(x => x.Reference).IsRequired().HasMaxLength(150);
        });

        mb.Entity<ContentItem>(e =>
        {
            e.HasKey(x => x.ContentItemId);
            e.Property(x => x.Title).IsRequired().HasMaxLength(150).UseCollation("NOCASE");
            e.Property(x => x.CulturalDomain).HasMaxLength(30);
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt).IsRequired();

            // Unique title within a category (case-insensitive via NOCASE).
            e.HasIndex(x => new { x.CategoryId, x.Title }).IsUnique();

            e.HasOne(x => x.Category).WithMany()
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany()
                .HasForeignKey(x => x.CreatedByReviewerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedReviewer).WithMany()
                .HasForeignKey(x => x.AssignedReviewerId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ContentItemSource>(e =>
        {
            e.ToTable("ContentItem_Source");
            e.HasKey(x => new { x.ContentItemId, x.SourceId });
            e.HasOne(x => x.ContentItem).WithMany(c => c.Sources)
                .HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Source).WithMany(s => s.ContentItems)
                .HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ValidationRecord>(e =>
        {
            e.HasKey(x => x.ValidationRecordId);
            e.Property(x => x.Decision).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DecidedAt).IsRequired();
            e.HasOne(x => x.ContentItem).WithMany(c => c.ValidationRecords)
                .HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Reviewer).WithMany()
                .HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}