using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public sealed class TargetDbContext : ApplicationDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public TargetDbContext(IOptions<ApplicationSettings> applicationSettings) : base(applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;

            // This is to improve quering time
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _applicationSettings.ConnectionStrings.Target,
                o => o.CommandTimeout(_applicationSettings.CommandTimeout));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TableIndex>().HasNoKey();
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<TableIndex> TableIndexes { get; set; }
    }
}
