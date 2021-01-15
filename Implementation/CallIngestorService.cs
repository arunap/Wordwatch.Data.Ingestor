using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Enums;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public sealed class CallIngestorService
    {
        private readonly IngestorInfoService _ingestorInfoService;
        private readonly IApplicationDbContext _sourceDbContext;
        private readonly IApplicationDbContext _targetDbContext;
        private readonly ApplicationSettings _applicationSettings;

        private DateTimeOffset? _callsLastSyncedAt;
        private DateTimeOffset? _mediaStubsLastSyncedAt;
        private DateTimeOffset? _voxStubsLastSyncedAt;

        public CallIngestorService(IApplicationDbContext sourceDbContext, IApplicationDbContext targetDbContext, ApplicationSettings applicationSettings)
        {
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings;
            _ingestorInfoService = new IngestorInfoService(_applicationSettings, sourceDbContext, targetDbContext);
        }

        private async Task<DateTimeOffset> GetLastSyncedAt(string tableName, Action<ProgressNotifier> notifyProgress)
        {
            var results = await _ingestorInfoService.IngestDataToInforTableAsync(notifyProgress);
            DateTimeOffset? lastSyncedAt = results.SyncedTableInfo.Where(x => x.RelatedTable == tableName).Select(x => x.LastSyncedAt).First();

            if (lastSyncedAt == null) // first time execution
            {
                if (tableName == Literals.SyncTableNames.CallsTable)
                {
                    lastSyncedAt = await _sourceDbContext.Calls.MinAsync(x => x.start_datetime);
                }
                if (tableName == Literals.SyncTableNames.MediaStubsTable)
                {
                    lastSyncedAt = await _sourceDbContext.MediaStubs.MinAsync(x => x.created);
                }
                if (tableName == Literals.SyncTableNames.VoxStubsTable)
                {
                    lastSyncedAt = await _sourceDbContext.VoxStubs.MinAsync(x => x.created);
                }
                lastSyncedAt = new DateTimeOffset(lastSyncedAt.Value.Date, DateTimeOffset.UtcNow.Offset);
            }
            else
            {
                lastSyncedAt = lastSyncedAt.Value.AddDays(1);
            }

            ResetLastSyncAt(tableName, lastSyncedAt, notifyProgress);

            return lastSyncedAt.Value;
        }

        private void ResetLastSyncAt(string tableName, DateTimeOffset? lastSyncedAt, Action<ProgressNotifier> notifyProgress)
        {
            if (tableName == Literals.SyncTableNames.CallsTable)
            {
                _callsLastSyncedAt = lastSyncedAt;
                notifyProgress(new ProgressNotifier { Field = UIFields.CallLastSyncedAt, FieldValue = lastSyncedAt });
            }
            if (tableName == Literals.SyncTableNames.MediaStubsTable)
            {
                _mediaStubsLastSyncedAt = lastSyncedAt;
                notifyProgress(new ProgressNotifier { Field = UIFields.MediaStubsLastSyncedAt, FieldValue = lastSyncedAt });
            }
            if (tableName == Literals.SyncTableNames.VoxStubsTable)
            {
                _voxStubsLastSyncedAt = lastSyncedAt;
                notifyProgress(new ProgressNotifier { Field = UIFields.VoxStubsLastSyncedAt, FieldValue = lastSyncedAt });
            }
        }

        private async Task UpdateSyncedTableInfoAsync(string tableName, DateTimeOffset lastSyncedAt)
        {
            string sql = $"UPDATE [dbo].[SyncedTableInfo] SET LastSyncedAt = '{lastSyncedAt}' WHERE RelatedTable = '{tableName}'";
            await _sourceDbContext.ExecuteRawSql(sql);
        }

        private async Task IngestTableRowsAsync(string tableName, Action<ProgressNotifier> notifyProgress)
        {
            var dateAt = await GetLastSyncedAt(tableName, notifyProgress);

            var min = dateAt;
            var max = dateAt.AddDays(1).AddSeconds(-1);

            if (tableName == Literals.SyncTableNames.CallsTable)
            {
                notifyProgress(new ProgressNotifier { Message = $"Started - Getting calls from source {min} - {max}" });
                List<Call> calls = await _sourceDbContext.Calls.Where(x => x.start_datetime >= min && x.start_datetime <= max).ToListAsync();

                notifyProgress(new ProgressNotifier { Message = $"Started - Inserting {calls.Count()} calls to target.", Field = UIFields.TargetIngestedCallCount, FieldValue = calls.Count() });
                await _targetDbContext.BatchInsertAsync(calls);
                notifyProgress(new ProgressNotifier { Message = $"Completed - Inserting calls to target." });
            }
            else if (tableName == Literals.SyncTableNames.MediaStubsTable)
            {
                notifyProgress(new ProgressNotifier { Message = $"Started - Getting media stubs from source {min} - {max}" });
                List<MediaStub> mediaStubs = await _sourceDbContext.MediaStubs.Where(x => x.created >= min && x.created <= max).ToListAsync();

                notifyProgress(new ProgressNotifier { Message = $"Started - Inserting {mediaStubs.Count()} stubs to target.", Field = UIFields.TargetIngestedMediaStubCount, FieldValue = mediaStubs.Count() });
                await _targetDbContext.BatchInsertAsync(mediaStubs);
                notifyProgress(new ProgressNotifier { Message = $"Completed - Inserting stubs to target." });
            }
            else if (tableName == Literals.SyncTableNames.VoxStubsTable)
            {
                notifyProgress(new ProgressNotifier { Message = $"Started - Getting vox stubs from source {min} - {max}" });
                List<VoxStub> voxStubs = await _sourceDbContext.VoxStubs.Where(x => x.created >= min && x.created <= max).ToListAsync();

                notifyProgress(new ProgressNotifier { Message = $"Started - Inserting {voxStubs.Count()} stubs to target.", Field = UIFields.TargetIngestedVoxStubCount, FieldValue = voxStubs.Count() });
                await _targetDbContext.BatchInsertAsync(voxStubs);
                notifyProgress(new ProgressNotifier { Message = $"Completed - Inserting stubs to target." });
            }

            ResetLastSyncAt(tableName, min, notifyProgress);

            await UpdateSyncedTableInfoAsync(tableName, min);
        }

        public async Task ExecuteIterationsAsync(string table, int count, Action<ProgressNotifier> notifyProgress)
        {
            for (int i = 0; i < 1000; i++)
            {
                await IngestTableRowsAsync(table, notifyProgress);
            }

            notifyProgress(new ProgressNotifier { Message = $"Source content moved to Target." });
        }
    }
}
