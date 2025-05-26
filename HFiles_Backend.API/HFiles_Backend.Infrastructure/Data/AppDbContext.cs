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

            modelBuilder.Entity<LabSignupUser>()
                .HasIndex(u => u.HFID)
                .IsUnique();

            modelBuilder.Entity<UserDetails>().HasNoKey();
        }

        public DbSet<UserReports> UserReports { get; set; }
        public DbSet<UserDetails> UserDetails { get; set; }
        public DbSet<LabUserReports> LabUserReports { get; set; }
        public DbSet<LabAdmin> LabAdmins { get; set; }
        public DbSet<LabMember> LabMembers { get; set; }
        


    }
}
