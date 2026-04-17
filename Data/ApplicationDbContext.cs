using Microsoft.EntityFrameworkCore;
using AdminMembers.Models;

namespace AdminMembers.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Disable change tracking for read-only queries (improves performance)
            // Note: Enable it explicitly when needed for updates
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<CustomField> CustomFields { get; set; }
        public DbSet<MemberCustomField> MemberCustomFields { get; set; }
        public DbSet<MemberDocument> MemberDocuments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<AgendaEvent> AgendaEvents { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<AiSettings> AiSettings { get; set; }
        public DbSet<FeatureRequest> FeatureRequests { get; set; }
        public DbSet<BoardReport> BoardReports { get; set; }
        public DbSet<BoardReportAttendee> BoardReportAttendees { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Performance: Add table-level index hints
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Members");
                
                // Indexes for performance
                entity.Property(e => e.MemberNumber).IsRequired(false);
                entity.HasIndex(e => e.MemberNumber).IsUnique();
                entity.HasIndex(e => e.LastName); // For sorting and searching
                entity.HasIndex(e => e.FirstName); // For searching
                entity.HasIndex(e => e.Email); // For searching
                entity.HasIndex(e => e.IsAlive); // For filtering
                
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.IsAlive).IsRequired();
                entity.Property(e => e.SeniorityDate).IsRequired(false);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Addresses");
                entity.Property(e => e.Street).IsRequired().HasMaxLength(200);
                entity.Property(e => e.City).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Country).HasMaxLength(100);

                entity.HasOne(a => a.Member)
                      .WithOne(m => m.Address)
                      .HasForeignKey<Address>(a => a.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AppSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("AppSettings");
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.LogoFileName).HasMaxLength(500);
                entity.Property(e => e.LogoContentType).HasMaxLength(100);
            });

            modelBuilder.Entity<CustomField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("CustomFields");
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldLabel).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.FieldName).IsUnique();
                entity.HasIndex(e => e.DisplayOrder); // For sorting
                entity.HasIndex(e => e.IsActive); // For filtering
            });

            modelBuilder.Entity<MemberCustomField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("MemberCustomFields");
                entity.Property(e => e.Value).HasMaxLength(1000);

                entity.HasOne(mcf => mcf.Member)
                      .WithMany(m => m.CustomFieldValues)
                      .HasForeignKey(mcf => mcf.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mcf => mcf.CustomField)
                      .WithMany(cf => cf.MemberValues)
                      .HasForeignKey(mcf => mcf.CustomFieldId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.MemberId, e.CustomFieldId }).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Users");
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.IsApproved).IsRequired().HasDefaultValue(true);
                entity.HasIndex(e => e.IsApproved);
                entity.Property(e => e.TotpSecret).HasMaxLength(64).IsRequired(false);
                entity.Property(e => e.TotpEnabled).IsRequired().HasDefaultValue(false);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Roles");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("UserRoles");
                
                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("AuditLogs");
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityType).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.HasIndex(e => e.Timestamp); // For date range queries
                entity.HasIndex(e => e.UserId); // For user-specific queries
                entity.HasIndex(e => e.Action); // For action filtering

                entity.HasOne(al => al.User)
                      .WithMany()
                      .HasForeignKey(al => al.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<MemberDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("MemberDocuments");
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.BlobName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UploadedByUsername).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.MemberId);

                entity.HasOne(d => d.Member)
                      .WithMany(m => m.Documents)
                      .HasForeignKey(d => d.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("StockItems");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CurrentStock).HasColumnType("decimal(18,3)");
                entity.Property(e => e.MinimumStock).HasColumnType("decimal(18,3)");
                entity.Ignore(e => e.Status);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("StockMovements");
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Property(e => e.CreatedByUsername).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MovementDate).IsRequired(false);
                entity.HasIndex(e => e.StockItemId);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(sm => sm.StockItem)
                      .WithMany(si => si.Movements)
                      .HasForeignKey(sm => sm.StockItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AgendaEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("AgendaEvents");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Color).HasMaxLength(20);
                entity.Property(e => e.CreatedByUsername).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.StartDate);
            });

            modelBuilder.Entity<EmailSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("EmailSettings");
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SmtpHost).HasMaxLength(200);
                entity.Property(e => e.Username).HasMaxLength(200);
                entity.Property(e => e.Password).HasMaxLength(500);
                entity.Property(e => e.ApiKey).HasMaxLength(500);
                entity.Property(e => e.FromAddress).HasMaxLength(200);
                entity.Property(e => e.FromName).HasMaxLength(200);
            });

            modelBuilder.Entity<AiSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("AiSettings");
                entity.Property(e => e.GitHubToken).HasMaxLength(500);
                entity.Property(e => e.GitHubOwner).HasMaxLength(100);
                entity.Property(e => e.GitHubRepo).HasMaxLength(100);
            });

            modelBuilder.Entity<FeatureRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("FeatureRequests");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.Priority).HasMaxLength(20);
                entity.Property(e => e.SubmittedByUsername).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.SubmittedByUserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<BoardReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("BoardReports");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedByUsername).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.MeetingDate);
                entity.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<BoardReportAttendee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("BoardReportAttendees");

                entity.HasOne(a => a.BoardReport)
                      .WithMany(r => r.Attendees)
                      .HasForeignKey(a => a.BoardReportId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Member)
                      .WithMany()
                      .HasForeignKey(a => a.MemberId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.BoardReportId, e.MemberId }).IsUnique();
            });

            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ApiKeys");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(12);
                entity.HasIndex(e => e.KeyHash).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Super Admin",     Description = "All rights including audit logs",          Permission = Permission.ReadWrite },
                new Role { Id = 2, Name = "Admin",           Description = "All rights except audit logs",             Permission = Permission.ReadWrite },
                new Role { Id = 3, Name = "Member Editor",   Description = "Can add, edit and delete members",         Permission = Permission.ReadWrite },
                new Role { Id = 4, Name = "Member Viewer",   Description = "Read-only access to members",              Permission = Permission.Read },
                new Role { Id = 5, Name = "Stock Editor",    Description = "Can add, edit and manage stock",           Permission = Permission.ReadWrite },
                new Role { Id = 6, Name = "Stock Viewer",    Description = "Read-only access to stock",                Permission = Permission.Read },
                new Role { Id = 7, Name = "Secretaris",      Description = "Secretary - read/write all homepage features", Permission = Permission.ReadWrite }
            );
        }
    }
}
