using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public sealed class TargetDbContext : ApplicationDbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        public TargetDbContext(IOptions<ApplicationSettings> applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _applicationSettings.ConnectionStrings.Target, 
                o => o.CommandTimeout(_applicationSettings.CommandTimeout)); 
        }
    }
}
