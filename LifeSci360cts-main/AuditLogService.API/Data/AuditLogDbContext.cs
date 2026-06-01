using AuditLogService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditLogService.API.Data
{
    public class AuditLogDbContext : DbContext
    {
        public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options)
            : base(options) { }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Action)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.Property(e => e.ServiceName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(e => e.UserEmail)
                      .HasMaxLength(150);

                // ── FIXED: Remove HasDefaultValue so EF Core
                //    always includes IsError in INSERT statement
                entity.Property(e => e.IsError)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETDATE()")
                      .HasColumnType("datetime");
            });
        }
    }
}