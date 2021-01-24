using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    [NotMapped]
    public class VoxStub : CallEntity
    {
        public Guid id { get; set; }
        public DateTimeOffset? modified { get; set; }
        public Guid location_id { get; set; }
        public string file_id { get; set; }
        public DateTimeOffset retention_expiry { get; set; }
        public string availability_status { get; set; }
        public string additional_data { get; set; }
        public string channel_key { get; set; }
    }
}
