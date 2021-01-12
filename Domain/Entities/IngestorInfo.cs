using System;
using System.ComponentModel.DataAnnotations.Schema;
using Wordwatch.Data.Ingestor.Application.Enums;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class IngestorInfo
    {
        public int id { get; set; }
        public Guid call_id { get; set; }
        public string channel_key { get; set; }
        public short call_type { get; set; }
        public DateTimeOffset start_datetime { get; set; }
        public DateTimeOffset stop_datetime { get; set; }
        public DataIngestStatus DataIngestStatus { get; set; }
        public bool SyncedToElastic { get; set; }
    }
}
