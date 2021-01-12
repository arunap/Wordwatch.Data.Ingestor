using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Wordwatch.Data.Ingestor.Application.Interfaces;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public class DestinationDbContext : ApplicationDbContext, ITargetDbContext
    {
        private string _connectionString;
        public DestinationDbContext()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["Destination"].ConnectionString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=SL-ARUNAP;Initial Catalog=wordwatch_tests;Integrated Security=True;Connect Timeout=0;"); //TODO
        }
    }
}
