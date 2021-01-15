using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Enums;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public class SourceDbContext : ApplicationDbContext, ISourceDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public SourceDbContext(ApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public DbSet<IngestorInfo> IngestorInfos { get; set; }
        public DbSet<SyncedTableInfo> SyncedTableInfo { get; set; }

        public async Task EnsureMigrationAsync(CancellationToken cancellationToken)
        {
            await this.Database.MigrateAsync(cancellationToken);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_applicationSettings.ConnectionStrings.Source, o => o.CommandTimeout(1000));
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IngestorInfo>().Property(x => x.id).ValueGeneratedOnAdd();

            modelBuilder.Entity<IngestorInfo>().Property(x => x.DataIngestStatus).HasDefaultValue<DataIngestStatus>(DataIngestStatus.Pending);

            modelBuilder.Entity<IngestorInfo>().Property(x => x.SyncedToElastic).HasDefaultValue<bool>(false);

            modelBuilder.Entity<SyncedTableInfo>().Property(x => x.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<SyncedTableInfo>().Property(x => x.RelatedTable).HasMaxLength(15);
            modelBuilder.Entity<SyncedTableInfo>().ToTable("SyncedTableInfo", "dbo");

            modelBuilder.Entity<IngestorInfo>().ToTable("IngestorInfo", "dbo");

            base.OnModelCreating(modelBuilder);
        }
    }
}

