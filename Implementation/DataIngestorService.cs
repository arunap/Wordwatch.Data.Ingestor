using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public class DataIngestorService : IDataIngestor
    {
        IProgress<CallIngestorInfo> _progressCallback;

        private readonly IApplicationDbContext _sourceDbContext;
        private readonly IApplicationDbContext _targetDbContext;

        private readonly int _ingestBatchSize = 1000;
        private readonly int _queringBatchSize = 100000;

        public SourceTableInfo _sourceDataSummary = new SourceTableInfo();

        private DataIngestorService()
        {

        }

        public DataIngestorService(IApplicationDbContext sourceDbContext, IApplicationDbContext targetDbContext)
        {
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;

            int.TryParse(ConfigurationManager.AppSettings["IngestBatchSize"], out _ingestBatchSize);
            int.TryParse(ConfigurationManager.AppSettings["QueringBatchSize"], out _queringBatchSize);
        }

        public void RegisterProgressCallBacks(IProgress<CallIngestorInfo> progressCallback)
        {
            _progressCallback = progressCallback;
        }

        public Task Pause(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task ResumeAync(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 50; i++)
            {
                NotifyProgress($"testmessage - {i}");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            await Task.CompletedTask;
        }

        public async Task StartAync(CancellationToken cancellationToken)
        {
            try
            {
                await InitIngestorInfoTable(cancellationToken);

                // await StartCallsDumping(cancellationToken);
            }
            catch (Exception ex)
            {
                NotifyProgress($"Error - {ex.Message}.");
            }
        }

        public Task StopAync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task StartCallsDumping(CancellationToken cancellationToken)
        {
            await GetPendingIngestorInfoAsync(cancellationToken);
        }

        private async Task GetPendingIngestorInfoAsync(CancellationToken cancellationToken)
        {
            int callCount = _sourceDataSummary.TotalCalls;
            int totalPages = callCount / _queringBatchSize;

            int pageNumber = 1;

            do
            {
                var ingestInfor = await _sourceDbContext.BatchReadAsync<IngestorInfo>(x => x.DataIngestStatus == Application.Enums.DataIngestStatus.Pending, null, "", pageNumber, _queringBatchSize);

                var call_ids_to_dump = ingestInfor.Select(x => x.call_id).ToArray();
                var media_ids_to_dump = ingestInfor.Where(x => x.call_type == 0).Select(x => x.call_id).ToArray();

                var callsToDump = await _sourceDbContext.BatchReadAsync<Call>(x => call_ids_to_dump.Contains(x.id), null, "", -1, _queringBatchSize);
                var mediaToDump = await _sourceDbContext.BatchReadAsync<MediaStub>(x => media_ids_to_dump.Contains(x.call_id), null, "", -1, _queringBatchSize);

                // insert to calls table
                await _targetDbContext.BatchInsertAsync<Call>(callsToDump.ToList(), cancellationToken);

                // insert to media stubs
                await _targetDbContext.BatchInsertAsync<MediaStub>(mediaToDump.ToList(), cancellationToken);

                pageNumber++;

            } while (pageNumber <= totalPages);
        }

        private async Task InitIngestorInfoTable(CancellationToken cancellationToken)
        {
            // 1 -migrations
            NotifyProgress("Started - Executing Migrations & Initiazing Summary table.");

            await ((ISourceDbContext)_sourceDbContext).EnsureMigrationAsync(cancellationToken);

            NotifyProgress("Completed - Executing Migrations & Initiazing Summary table.");

            // 2 - dump calls from source
            await DumpIngestorInforContent(cancellationToken);
        }

        private async Task DumpIngestorInforContent(CancellationToken cancellationToken)
        {
            NotifyProgress("Started - Ingesting call summary.");
            int ingestedInfoRowCount = await _sourceDbContext.TableRowCountAsync<IngestorInfo>();
            if (ingestedInfoRowCount > 0 && _sourceDataSummary.TotalCalls == ingestedInfoRowCount)
            {
                await Task.CompletedTask;
                NotifyProgress($"Skipped - {ingestedInfoRowCount} call detail summary already exists.");
                return;
            }

            int totalPages = _sourceDataSummary.TotalCalls / _queringBatchSize;
            int pageNumber = 1;

            do
            {
                IEnumerable<Call> callEntities = null;
                callEntities = await _sourceDbContext.BatchReadAsync<Call>(null, null, null, pageNumber, _queringBatchSize);

                var infor = callEntities.Select(x => new IngestorInfo
                {
                    call_id = x.id,
                    call_type = x.call_type,
                    channel_key = x.channel_key,
                    start_datetime = x.start_datetime,
                    stop_datetime = x.stop_datetime,
                    SyncedToElastic = false,
                    DataIngestStatus = Application.Enums.DataIngestStatus.Pending
                }).ToList();

                await _sourceDbContext.BatchInsertAsync<IngestorInfo>(infor, cancellationToken);

                NotifyProgress($"{pageNumber}/{totalPages} Inserted call snapshot to IngestorInfo table.");

                pageNumber++;

            } while (pageNumber <= totalPages);

            NotifyProgress("Completed - Inserting call snapshot to IngestorInfo table.");
        }

        private void NotifyProgress(string message)
        {
            _progressCallback?.Report(new CallIngestorInfo { Message = message });
        }

        public async Task<SourceTableInfo> GetSourceDataSummaryAsync()
        {
            NotifyProgress($"Started - Loading source call tables data information.");
            var totalCalls = await _sourceDbContext.TableRowCountAsync<Call>();

            NotifyProgress($"Started - Loading source media stubs tables data information.");
            var totalMediaStubs = await _sourceDbContext.TableRowCountAsync<MediaStub>();

            NotifyProgress($"Started - Loading source vox stubs tables data information.");
            var totalVoxStubs = await _sourceDbContext.TableRowCountAsync<VoxStub>();

            NotifyProgress("Completed - Loading source tables data information.");

            _sourceDataSummary.TotalCalls = totalCalls;
            _sourceDataSummary.TotalMediaStubs = totalMediaStubs;
            _sourceDataSummary.TotalVoxStubs = totalVoxStubs;

            return _sourceDataSummary;
        }
    }
}

