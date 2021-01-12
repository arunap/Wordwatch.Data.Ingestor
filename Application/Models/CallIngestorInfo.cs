using System;
using System.Configuration;

namespace Wordwatch.Data.Ingestor.Application.Models
{
    public class CallIngestorInfo
    {
        public int TotalCalls { get; set; }
        public int IngestBatchSize { get; private set; } = int.Parse(ConfigurationManager.AppSettings["IngestBatchSize"]);
        public int IngestoredCalls { get; set; }
        public string Message { get; set; }
    }

    public class SourceDataSummary
    {
        public int TotalCalls { get; set; }
        public int TotalMediaStubs { get; set; }
        public int TotalVoxStubs { get; set; }

        public override string ToString()
        {
            return $"Calls: {TotalCalls}{Environment.NewLine}Media Stubs: {TotalMediaStubs}{Environment.NewLine}Vox Stubs: {TotalVoxStubs}";
        }
    }
}
