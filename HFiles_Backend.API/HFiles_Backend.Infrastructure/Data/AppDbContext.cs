using Microsoft.EntityFrameworkCore;
using HFiles_Backend.Domain.Entities.Labs;

namespace HFiles_Backend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<LabSignupUser> LabSignupUsers { get; set; }
        public DbSet<LabOtpEntry> LabOtpEntries { get; set; }

        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Marks HFID Unique
            modelBuilder.Entity<LabSignupUser>()
                .HasIndex(u => u.HFID)
                .IsUnique();

            // Mark user_details as read-only (no migrations)
            modelBuilder.Entity<UserDetails>().HasNoKey();
        }

        public DbSet<UserReports> UserReports { get; set; }
        public DbSet<LabUserReports> LabUserReports { get; set; }
        public DbSet<LabAdmin> LabAdmins { get; set; }


    }
}
