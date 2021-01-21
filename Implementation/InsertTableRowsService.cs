﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public sealed class InsertTableRowsService
    {
        private DateTimeOffset _callsLastSyncedAt = DateTimeOffset.MinValue;
        private DateTimeOffset _mediaStubsLastSyncedAt = DateTimeOffset.MinValue;
        private DateTimeOffset _voxStubsLastSyncedAt = DateTimeOffset.MinValue;
        private readonly ILogger<InsertTableRowsService> _logger;
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ApplicationSettings _applicationSettings;

        public InsertTableRowsService(
            ILogger<InsertTableRowsService> logger, IOptions<ApplicationSettings> applicationSettings,
            SourceDbContext sourceDbContext, TargetDbContext targetDbContext)
        {
            _logger = logger;
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings.Value;
        }

        private async Task IngestTableRowsAsync(string tableName, IProgress<ProgressNotifier> notifyProgress)
        {
            var dateAt = await GetLastSyncedAt(tableName, notifyProgress);

            var min = dateAt;
            var max = dateAt.AddDays(1).AddSeconds(-1);

            if (tableName == SyncTableNames.CallsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Reading} - source calls {min} - {max}" });
                List<Call> calls = await _sourceDbContext.Calls.Where(x => x.start_datetime >= min && x.start_datetime <= max).AsNoTracking().ToListAsync();

                if (calls.Count > 0)
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"{ MigrationMessageActions.Migrating } - calls to target.", Field = UIFields.TargetIngestedCallCount, FieldValue = calls.Count() });
                    await _targetDbContext.BatchInsertAsync(calls, default);
                    notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Migrated} - {calls.Count()} call rows." });
                }
                else
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"No Calls to Insert." });
                }
            }
            else if (tableName == SyncTableNames.MediaStubsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Message = $"{ MigrationMessageActions.Reading } - source media_stubs  { min} - {max}" });
                List<MediaStub> mediaStubs = await _sourceDbContext.MediaStubs.Where(x => x.created >= min && x.created <= max).AsNoTracking().ToListAsync();

                if (mediaStubs.Count > 0)
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"{ MigrationMessageActions.Migrating } - media stubs to target.", Field = UIFields.TargetIngestedMediaStubCount, FieldValue = mediaStubs.Count() });
                    await _targetDbContext.BatchInsertAsync(mediaStubs, default);
                    notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Migrated} - {mediaStubs.Count()} media rows." });
                }
                else
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"No Media Stubs to Insert." });
                }
            }
            else if (tableName == SyncTableNames.VoxStubsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Reading} - source vox_stubs {min} - {max}" });
                List<VoxStub> voxStubs = await _sourceDbContext.VoxStubs.Where(x => x.start_datetime >= min && x.start_datetime <= max).AsNoTracking().ToListAsync();

                if (voxStubs.Count > 0)
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"{ MigrationMessageActions.Migrating } - vox stubs to target.", Field = UIFields.TargetIngestedVoxStubCount, FieldValue = voxStubs.Count() });
                    await _targetDbContext.BatchInsertAsync(voxStubs, default);
                    notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Migrated} - {voxStubs.Count()} vox rows." });
                }
                else
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"No Vox Stubs to Insert." });
                }
            }

            ResetLastSyncAt(tableName, min, notifyProgress);

            await UpdateSyncedTableInfoAsync(tableName, min);
        }

        private async Task<DateTimeOffset> GetLastSyncedAt(string tableName, IProgress<ProgressNotifier> notifyProgress)
        {
            DateTimeOffset? lastSyncedAt = _sourceDbContext.SyncedTableInfo.Where(x => x.RelatedTable == tableName).Select(x => x.LastSyncedAt).First();

            if (lastSyncedAt == null) // first time execution
            {
                if (tableName == SyncTableNames.CallsTable)
                {
                    lastSyncedAt = await _sourceDbContext.Calls.MinAsync(x => x.start_datetime);
                }
                if (tableName == SyncTableNames.MediaStubsTable)
                {
                    lastSyncedAt = await _sourceDbContext.MediaStubs.MinAsync(x => x.created);
                }
                if (tableName == SyncTableNames.VoxStubsTable)
                {
                    lastSyncedAt = await _sourceDbContext.VoxStubs.MinAsync(x => x.start_datetime);
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

        private void ResetLastSyncAt(string tableName, DateTimeOffset? lastSyncedAt, IProgress<ProgressNotifier> notifyProgress)
        {
            if (tableName == SyncTableNames.CallsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Field = UIFields.CallLastSyncedAt, FieldValue = lastSyncedAt });
            }
            if (tableName == SyncTableNames.MediaStubsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Field = UIFields.MediaStubsLastSyncedAt, FieldValue = lastSyncedAt });
            }
            if (tableName == SyncTableNames.VoxStubsTable)
            {
                notifyProgress.Report(new ProgressNotifier { Field = UIFields.VoxStubsLastSyncedAt, FieldValue = lastSyncedAt });
            }
        }

        private async Task UpdateSyncedTableInfoAsync(string tableName, DateTimeOffset lastSyncedAt)
        {
            string sql = $"UPDATE [dbo].[SyncedTableInfo] SET LastSyncedAt = '{lastSyncedAt}' WHERE RelatedTable = '{tableName}'";
            await _sourceDbContext.ExecuteRawSql(sql);
        }

        public async Task InsertRowsAsync(string table, IProgress<ProgressNotifier> notifyProgress)
        {
            await IngestTableRowsAsync(table, notifyProgress);
        }
    }
}
