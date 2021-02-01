using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class SyncedTableInfo : BaseEntity
    {
        public int Id { get; set; }
        public DateTimeOffset? LastSyncedAt { get; set; }
        public string RelatedTable { get; set; }
    }
}
