using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public sealed class TargetDbContext : ApplicationDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public TargetDbContext(IOptions<ApplicationSettings> applicationSettings) : base(applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;
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
