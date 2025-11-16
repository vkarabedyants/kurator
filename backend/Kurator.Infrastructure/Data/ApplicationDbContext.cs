using Microsoft.EntityFrameworkCore;
using Kurator.Core.Entities;
using Kurator.Core.Enums;

namespace Kurator.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<BlockCurator> BlockCurators => Set<BlockCurator>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    public DbSet<InfluenceStatusHistory> InfluenceStatusHistories => Set<InfluenceStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Watchlist> Watchlists => Set<Watchlist>();
    public DbSet<WatchlistHistory> WatchlistHistories => Set<WatchlistHistory>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<ReferenceValue> ReferenceValues => Set<ReferenceValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Login).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.PublicKey).HasMaxLength(4000);
            entity.Property(e => e.MfaSecret).HasMaxLength(500);
            entity.HasIndex(e => e.Login).IsUnique();
        });

        // Block configuration
        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("blocks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // BlockCurator configuration (таблица связи кураторов с блоками)
        modelBuilder.Entity<BlockCurator>(entity =>
        {
            entity.ToTable("block_curator");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CuratorType).HasConversion<string>();

            // Один куратор не может быть дважды в одном блоке
            entity.HasIndex(e => new { e.BlockId, e.UserId }).IsUnique();
            // В блоке может быть только один Primary и один Backup куратор
            entity.HasIndex(e => new { e.BlockId, e.CuratorType }).IsUnique();

            entity.HasOne(e => e.Block)
                .WithMany(b => b.CuratorAssignments)
                .HasForeignKey(e => e.BlockId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.BlockAssignments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Contact configuration
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContactId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FullNameEncrypted).IsRequired();
            entity.HasIndex(e => e.ContactId).IsUnique();
            entity.HasIndex(e => e.BlockId);
            entity.HasIndex(e => e.NextTouchDate);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Block)
                .WithMany(b => b.Contacts)
                .HasForeignKey(e => e.BlockId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ResponsibleCurator)
                .WithMany(u => u.Contacts)
                .HasForeignKey(e => e.ResponsibleCuratorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Связи со справочниками
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.InfluenceStatus)
                .WithMany()
                .HasForeignKey(e => e.InfluenceStatusId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.InfluenceType)
                .WithMany()
                .HasForeignKey(e => e.InfluenceTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CommunicationChannel)
                .WithMany()
                .HasForeignKey(e => e.CommunicationChannelId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ContactSource)
                .WithMany()
                .HasForeignKey(e => e.ContactSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Interaction configuration
        modelBuilder.Entity<Interaction>(entity =>
        {
            entity.ToTable("interactions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContactId);
            entity.HasIndex(e => e.InteractionDate);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Interactions)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Curator)
                .WithMany(u => u.Interactions)
                .HasForeignKey(e => e.CuratorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Связи со справочниками
            entity.HasOne(e => e.InteractionType)
                .WithMany()
                .HasForeignKey(e => e.InteractionTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Result)
                .WithMany()
                .HasForeignKey(e => e.ResultId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InfluenceStatusHistory configuration
        modelBuilder.Entity<InfluenceStatusHistory>(entity =>
        {
            entity.ToTable("influence_status_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreviousStatus).HasMaxLength(10);
            entity.Property(e => e.NewStatus).HasMaxLength(10);
            entity.HasIndex(e => e.ContactId);
            entity.HasIndex(e => e.ChangedAt);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.StatusHistory)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedBy)
                .WithMany(u => u.InfluenceStatusChanges)
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasConversion<string>();
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Watchlist configuration
        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.ToTable("watchlist");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RoleStatus).HasMaxLength(200);
            entity.Property(e => e.RiskLevel).HasConversion<string>();
            entity.Property(e => e.MonitoringFrequency).HasConversion<string>();
            entity.HasIndex(e => e.NextCheckDate);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.WatchOwner)
                .WithMany(u => u.WatchlistItems)
                .HasForeignKey(e => e.WatchOwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RiskSphere)
                .WithMany()
                .HasForeignKey(e => e.RiskSphereId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // WatchlistHistory configuration
        modelBuilder.Entity<WatchlistHistory>(entity =>
        {
            entity.ToTable("watchlist_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OldRiskLevel).HasConversion<string>();
            entity.Property(e => e.NewRiskLevel).HasConversion<string>();
            entity.HasIndex(e => e.WatchlistId);
            entity.HasIndex(e => e.ChangedAt);

            entity.HasOne(e => e.Watchlist)
                .WithMany(w => w.History)
                .HasForeignKey(e => e.WatchlistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany(u => u.WatchlistChanges)
                .HasForeignKey(e => e.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FAQ configuration
        modelBuilder.Entity<FAQ>(entity =>
        {
            entity.ToTable("faqs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ReferenceValue configuration
        modelBuilder.Entity<ReferenceValue>(entity =>
        {
            entity.ToTable("reference_values");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => new { e.Category, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.Category, e.IsActive });
            entity.HasIndex(e => e.SortOrder);
        });
    }
}
