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
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full access with read and write permissions", Permission = Permission.ReadWrite },
                new Role { Id = 2, Name = "Editor", Description = "Can read and write data", Permission = Permission.ReadWrite },
                new Role { Id = 3, Name = "Viewer", Description = "Read-only access", Permission = Permission.Read }
            );
        }
    }
}
