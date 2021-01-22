using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public sealed class SourceDbContext : ApplicationDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public SourceDbContext(IOptions<ApplicationSettings> applicationSettings) : base(applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;

            // This is to improve quering time
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public DbSet<SyncedTableInfo> SyncedTableInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    _applicationSettings.ConnectionStrings.Source,
                    o => o.CommandTimeout(_applicationSettings.CommandTimeout));
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SyncedTableInfo>().ToTable("SyncedTableInfo", TableSchemaNames.Dbo);

            base.OnModelCreating(modelBuilder);
        }
    }
}

