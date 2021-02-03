using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class SyncedTableInfo : BaseEntity
    {
        public int Id { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string RelatedTable { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public int DaysPending { get; set; }
    }
}
