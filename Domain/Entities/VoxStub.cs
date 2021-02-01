using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class VoxStub : BaseEntity
    {
        // public Guid id { get; set; } ==> will generate in the Db as NEWSEQUENTIALID
        public DateTimeOffset? modified { get; set; }
        public Guid location_id { get; set; }
        public string file_id { get; set; }
        public DateTimeOffset retention_expiry { get; set; }
        public string availability_status { get; set; }
        public string additional_data { get; set; }
        public string channel_key { get; set; }
        public DateTimeOffset created { get; set; }
        public DateTimeOffset start_datetime { get; set; }
        public DateTimeOffset stop_datetime { get; set; }
    }
}
