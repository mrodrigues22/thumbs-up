using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<MediaFile> MediaFiles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure Submission
        builder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AccessToken).IsUnique();
            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.Submissions)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.MediaFiles)
                .WithOne(m => m.Submission)
                .HasForeignKey(m => m.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Review)
                .WithOne(r => r.Submission)
                .HasForeignKey<Review>(r => r.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure MediaFile
        builder.Entity<MediaFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SubmissionId);
        });
        
        // Configure Review
        builder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SubmissionId).IsUnique();
        });
    }
}
