using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class MediaStub : BaseEntity
    {
        // public Guid id { get; set; } ==> will generate in the Db as NEWSEQUENTIALID
        public Guid call_id { get; set; }
        public string stub_type { get; set; }
        public DateTimeOffset created { get; set; }
        public DateTimeOffset? modified { get; set; }
        public Guid location_id { get; set; }
        public string file_id { get; set; }
        public DateTimeOffset? retention_expiry { get; set; }
    }
}
