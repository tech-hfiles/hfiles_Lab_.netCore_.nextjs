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

        // Makes HFID unique
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LabSignupUser>()
                .HasIndex(u => u.HFID)
                .IsUnique();
        }



    }
}
