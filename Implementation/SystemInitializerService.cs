﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public sealed class SystemInitializerService
    {
        private readonly ILogger<SystemInitializerService> _logger;
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ApplicationSettings _applicationSettings;
        private MigrationTableInfo _sourceTableInfo;
        private MigrationTableInfo _targetTableInfo;

        public SystemInitializerService(
            ILogger<SystemInitializerService> logger, IOptions<ApplicationSettings> applicationSettings,
            SourceDbContext sourceDbContext, TargetDbContext targetDbContext )
        {
            _sourceDbContext = sourceDbContext;
            _applicationSettings = applicationSettings.Value;
            _targetDbContext = targetDbContext;

            _sourceTableInfo = new MigrationTableInfo();
            _targetTableInfo = new MigrationTableInfo();
            _logger = logger;
        }

        private async Task<List<SyncedTableInfo>> InitSyncedInfoTableAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            bool isExists = await _sourceDbContext.TableExistsAsync<SyncedTableInfo>();

            if (!isExists)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("CREATE TABLE [dbo].[SyncedTableInfo]([Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,[LastSyncedAt] [datetimeoffset](7) NULL,[RelatedTable] [varchar](15) NULL) ON [PRIMARY];");
                sb.Append("INSERT INTO [dbo].[SyncedTableInfo] ([RelatedTable] ) VALUES ('ww.calls'), ('ww.media_stubs'),('ww.vox_stubs');");
                sb.Append("IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_calls_crtd' AND object_id = OBJECT_ID('ww.calls')) BEGIN CREATE NONCLUSTERED INDEX [IX_calls_crtd] ON [ww].[calls] ([created] ASC) ON [PRIMARY] END");
                await _sourceDbContext.ExecuteRawSql(sb.ToString());
            }
            notifyProgress.Report(new ProgressNotifier { Message = "[dbo].[SyncedTableInfo] Initialized." });
            return await _sourceDbContext.SyncedTableInfo.ToListAsync();
        }

        private async Task<MigrationTableInfo> GetTargetDataSummaryAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            var totalCalls = await _targetDbContext.TableRowCountByIdAsync<Call>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading TARGET call tables data information.", Field = UIFields.TargetIngestedCallCount, FieldValue = totalCalls });

            var totalMediaStubs = await _targetDbContext.TableRowCountByIdAsync<MediaStub>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading TARGET media stubs tables data information.", Field = UIFields.TargetIngestedMediaStubCount, FieldValue = totalMediaStubs });

            var totalVoxStubs = await _targetDbContext.TableRowCountByIdAsync<VoxStub>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading TARGET vox stubs tables data information.", Field = UIFields.TargetIngestedVoxStubCount, FieldValue = totalVoxStubs });

            _targetTableInfo.TotalCalls = totalCalls;
            _targetTableInfo.TotalMediaStubs = totalMediaStubs;
            _targetTableInfo.TotalVoxStubs = totalVoxStubs;

            return _targetTableInfo;
        }

        private async Task<MigrationTableInfo> GetSourceDataSummaryAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            var totalCalls = await _sourceDbContext.TableRowCountByIdAsync<Call>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading SOURCE call tables data information.", Field = UIFields.SourceCallCount, FieldValue = totalCalls });

            var totalMediaStubs = await _sourceDbContext.TableRowCountByIdAsync<MediaStub>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading SOURCE media stubs tables data information.", Field = UIFields.SourceMediaStubCount, FieldValue = totalMediaStubs });

            var totalVoxStubs = await _sourceDbContext.TableRowCountByIdAsync<VoxStub>();
            notifyProgress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Loading SOURCE vox stubs tables data information.", Field = UIFields.SourceVoxStubCount, FieldValue = totalVoxStubs });

            // call distribution
            var callMinDate = await _sourceDbContext.Calls.MinAsync(x => x.start_datetime);
            notifyProgress.Report(new ProgressNotifier { Field = UIFields.CallsMinDate, FieldValue = callMinDate });

            var callMaxDate = await _sourceDbContext.Calls.MaxAsync(x => x.start_datetime);
            notifyProgress.Report(new ProgressNotifier { Field = UIFields.CallsMaxDate, FieldValue = callMaxDate });

            notifyProgress.Report(new ProgressNotifier { Field = UIFields.SourceCallDistribution, FieldValue = Math.Round((callMaxDate - callMinDate).TotalDays) });

            _sourceTableInfo.TotalCalls = totalCalls;
            _sourceTableInfo.TotalMediaStubs = totalMediaStubs;
            _sourceTableInfo.TotalVoxStubs = totalVoxStubs;

            return _sourceTableInfo;
        }

        public async Task<List<SyncedTableInfo>> InitTableAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            // init source & target table info
            if (_sourceTableInfo.TotalCalls == 0)
            {
                _sourceTableInfo = await GetSourceDataSummaryAsync(notifyProgress);
                _targetTableInfo = await GetTargetDataSummaryAsync(notifyProgress);
            }

            // init sync info table
            List<SyncedTableInfo> _syncedTableInfo = await InitSyncedInfoTableAsync(notifyProgress);
            notifyProgress.Report(new ProgressNotifier { Message= $"{MigrationMessageActions.Completed} Loading Table summeries."});

            return _syncedTableInfo;
        }
    }
}