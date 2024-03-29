﻿using System;

namespace Wordwatch.Data.Ingestor.Domain.Entities
{
    public class Call : BaseEntity
	{
		public Guid id { get; set; }
		public Guid? originating_device_id { get; set; }
		public Guid? terminating_device_id { get; set; }
		public string caller { get; set; }
		public string called { get; set; }
		public string direction { get; set; }
		public Guid? user_id { get; set; }
		public DateTimeOffset? modified { get; set; }
		public int bookmarks_count { get; set; }
		public int comments_count { get; set; }
		public DateTimeOffset? retention_expiry { get; set; }
		public string availability_status { get; set; }
		public string additional_data { get; set; }
		public Guid? association_id { get; set; }
		public string channel_key { get; set; }
		public short call_type { get; set; }
		public DateTimeOffset created { get; set; }
		public DateTimeOffset start_datetime { get; set; }
		public DateTimeOffset stop_datetime { get; set; }
	}
}
