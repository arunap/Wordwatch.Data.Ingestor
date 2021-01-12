using System;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Application.Interfaces
{
    public interface IDataIngestor
    {
        public Task StartAync(CancellationToken cancellationToken);

        public Task Pause(CancellationToken cancellationToken);

        public Task ResumeAync(CancellationToken cancellationToken);

        public Task StopAync(CancellationToken cancellationToken);

        public void RegisterProgressCallBacks(IProgress<CallIngestorInfo> progressCallback);

        public Task<SourceDataSummary> GetSourceDataSummaryAsync();
    }
}
