using System;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class ApplicationSettings
    {
        public int IngestBatchSize { get; set; } = 1000;
        public int QueringBatchSize { get; set; } = 10000;
        public Guid? StorageLocationId { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
    }
    public class ConnectionStrings
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }
}
