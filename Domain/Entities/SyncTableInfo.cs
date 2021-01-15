using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class SyncedTableInfo
    {
        public int Id { get; set; }
        public DateTimeOffset? LastSyncedAt { get; set; }
        public string RelatedTable { get; set; }
    }
}
