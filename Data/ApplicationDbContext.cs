using Microsoft.EntityFrameworkCore;
using AdminMembers.Models;

namespace AdminMembers.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<CustomField> CustomFields { get; set; }
        public DbSet<MemberCustomField> MemberCustomFields { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MemberNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.MemberNumber).IsUnique();
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
                entity.Property(e => e.CompanyName).HasMaxLength(200);
                entity.Property(e => e.LogoFileName).HasMaxLength(500);
                entity.Property(e => e.LogoContentType).HasMaxLength(100);
            });

            modelBuilder.Entity<CustomField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldLabel).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.FieldName).IsUnique();
            });

            modelBuilder.Entity<MemberCustomField>(entity =>
            {
                entity.HasKey(e => e.Id);
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
        }
    }
}
