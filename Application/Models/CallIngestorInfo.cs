using System;
using System.Collections.Generic;
using System.Configuration;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class CallIngestorInfo
    {
        public int TotalCalls { get; set; }
        public int IngestBatchSize { get; private set; } = 1000;
        public int IngestoredCalls { get; set; }
        public string Message { get; set; }
    }

    public class SourceTableInfo
    {
        public int TotalCalls { get; set; }
        public int TotalMediaStubs { get; set; }
        public int TotalVoxStubs { get; set; }

        public override string ToString()
        {
            return $"Calls: {TotalCalls}{Environment.NewLine}Media Stubs: {TotalMediaStubs}{Environment.NewLine}Vox Stubs: {TotalVoxStubs}";
        }
    }

    public class IngestTableInfor
    {
        public int TotalRowCount { get; set; }
    }

    public class SourceDbTableInfo
    {
        public IngestTableInfor IngestTableInfor { get; set; }
        public SourceTableInfo SourceTableInfo { get; set; }
        public SourceTableInfo TargetTableInfo { get; set; }
        public List<SyncedTableInfo> SyncedTableInfo { get; set; }
    }
}
