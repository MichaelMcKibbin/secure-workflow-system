using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace secure_workflow_system.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Case> Cases => Set<Case>();
        public DbSet<CaseStatusHistory> CaseStatusHistories => Set<CaseStatusHistory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Case>(entity =>
            {
                entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
                entity.Property(c => c.Description).HasMaxLength(4000).IsRequired();
                entity.Property(c => c.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(c => c.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(c => new { c.CreatedByUserId, c.CreatedAtUtc });
                entity.HasIndex(c => c.AssignedToUserId);
            });

            builder.Entity<CaseStatusHistory>(entity =>
            {
                entity.Property(h => h.OldStatus).HasMaxLength(50).IsRequired();
                entity.Property(h => h.NewStatus).HasMaxLength(50).IsRequired();

                entity.HasOne(h => h.Case)
                    .WithMany()
                    .HasForeignKey(h => h.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(h => h.CaseId);
                entity.HasIndex(h => h.ChangedByUserId);
                entity.HasIndex(h => new { h.CaseId, h.ChangedAtUtc });
            });
        }
    }
}
