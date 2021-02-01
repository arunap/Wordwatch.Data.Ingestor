using System;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class ApplicationSettings
    {
        public int IngestBatchSize { get; set; } = 1000;
        public int QueringBatchSize { get; set; } = 10000;
        public Guid? StorageLocationId { get; set; }
        public int CommandTimeout { get; set; } = 60 * 60; // 1hour
        public ConnectionStrings ConnectionStrings { get; set; }
        public BackendSettings BackendSettings { get; set; }
    }
    public class ConnectionStrings
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }

    public class BackendSettings
    {
        public bool SourcePKBuildRequired { get; set; } = true;
        public bool TargetPKBuildRequired { get; set; } = true;
        public int PKIndexBuildInterval { get; set; } = 10;
        public string[] SourceIdxToBuild { get; set; }
        public string[] TargetIdxToBuild { get; set; }
        public string NonClusteredIdxStatusQuery { get; set; }
        public string[] TargetDefaultConstraints { get; set; }
        public string[] FKConstraintsDisableQuery { get; set; }
        public string[] FKConstraintsEnableQuery { get; set; }
    }
}
