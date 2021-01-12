using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class CallEntity
    {
        public DateTimeOffset created { get; set; }
        public DateTimeOffset start_datetime { get; set; }
        public DateTimeOffset stop_datetime { get; set; }
    }
}
