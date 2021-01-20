using System.Collections.Generic;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class MigrationSummary
    {
        public MigrationTableInfo SourceTableInfo { get; set; }
        public MigrationTableInfo TargetTableInfo { get; set; }
        public List<SyncedTableInfo> SyncedTableInfo { get; set; }
        public int NoOfRows { get; set; }
    }
}
