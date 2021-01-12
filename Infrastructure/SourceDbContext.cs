using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Enums;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public class SourceDbContext : ApplicationDbContext, ISourceDbContext
    {
        private string _connectionString;
        public SourceDbContext()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["source"].ConnectionString;
        }

        public DbSet<IngestorInfo> IngestorInfos { get; set; }

        public async Task EnsureMigrationAsync(CancellationToken cancellationToken)
        {
            await this.Database.MigrateAsync(cancellationToken);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                SqlServerDbContextOptionsBuilder b = new SqlServerDbContextOptionsBuilder(optionsBuilder);
                b.CommandTimeout(1000);
                b.MigrationsHistoryTable("__DataMigrationsHistory", "dbo");

                optionsBuilder.UseSqlServer(_connectionString);
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IngestorInfo>().Property(x => x.id).ValueGeneratedOnAdd();

            modelBuilder.Entity<IngestorInfo>().Property(x => x.DataIngestStatus).HasDefaultValue<DataIngestStatus>(DataIngestStatus.Pending);

            modelBuilder.Entity<IngestorInfo>().Property(x => x.SyncedToElastic).HasDefaultValue<bool>(false);

            modelBuilder.Entity<IngestorInfo>().ToTable("IngestorInfo", "dbo");

            base.OnModelCreating(modelBuilder);
        }
    }
}

