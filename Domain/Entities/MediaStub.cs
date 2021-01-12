using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
	[NotMapped]
	public class MediaStub
	{
		public Guid id { get; set; }
		public Guid call_id { get; set; }
		public string stub_type { get; set; }
		public DateTimeOffset created { get; set; }
		public DateTimeOffset? modified { get; set; }
		public Guid location_id { get; set; }
		public string file_id { get; set; }
		public DateTimeOffset? retention_expiry { get; set; }
	}
}
