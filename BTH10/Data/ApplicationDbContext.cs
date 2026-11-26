using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BTH7.Models;

namespace BTH7.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<Lecturer> Lecturers { get; set; }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<BTH7.Models.Faculty> Faculty { get; set; } = default!;
        public DbSet<DynamicPermission> DynamicPermissions { get; set; } = default!;
        public DbSet<RolePermission> RolePermissions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Student>().Ignore(student => student.AdmissionYear);
            modelBuilder.Entity<DynamicPermission>(entity =>
            {
                entity.HasKey(permission => permission.Id);
                entity.HasIndex(permission => new { permission.Controller, permission.Action })
                    .IsUnique();
            });
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rolePermission => new
                {
                    rolePermission.RoleId,
                    rolePermission.PermissionId
                });
                entity.HasOne(rolePermission => rolePermission.Role)
                    .WithMany()
                    .HasForeignKey(rolePermission => rolePermission.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(rolePermission => rolePermission.Permission)
                    .WithMany(permission => permission.RolePermissions)
                    .HasForeignKey(rolePermission => rolePermission.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}