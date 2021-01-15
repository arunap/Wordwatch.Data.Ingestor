using Microsoft.EntityFrameworkCore;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public class DestinationDbContext : ApplicationDbContext, ITargetDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public DestinationDbContext(ApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_applicationSettings.ConnectionStrings.Target, o => o.CommandTimeout(1000)); //TODO
        }
    }
}
