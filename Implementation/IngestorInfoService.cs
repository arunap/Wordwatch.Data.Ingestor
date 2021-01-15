using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Enums;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public class IngestorInfoService
    {
        private ApplicationSettings _applicationSettings;
        private readonly IApplicationDbContext _sourceDbContext;
        private readonly IApplicationDbContext _targetDbContext;
        private SourceTableInfo _sourceTableInfo = new SourceTableInfo();
        private SourceTableInfo _targetTableInfo = new SourceTableInfo();
        private IngestTableInfor _ingestTableInfor = new IngestTableInfor();
        private List<SyncedTableInfo> _syncedTableInfo = new List<SyncedTableInfo>();

        public IngestorInfoService(ApplicationSettings settings, IApplicationDbContext sourceDbContext, IApplicationDbContext targetDbContext)
        {
            _sourceDbContext = sourceDbContext;
            _applicationSettings = settings;
            _targetDbContext = targetDbContext;
        }

        //private async Task<IngestTableInfor> InitIngestorInforTable()
        //{
        //    int ingestorTableRowCount = 0;
        //    bool isExists = await _sourceDbContext.TableExistsAsync<IngestorInfo>();
        //    if (isExists)
        //    {
        //        _notifyProgress("IngestorInfo Table is already exists in source database.");

        //        ingestorTableRowCount = await _sourceDbContext.TableRowCountAsync<IngestorInfo>();
        //        if (ingestorTableRowCount > 0 && _sourceTableInfo.TotalCalls == ingestorTableRowCount)
        //        {
        //            _notifyProgress($"Skipped - {ingestorTableRowCount} call detail summary already exists.");
        //        }
        //        else
        //        {
        //            _notifyProgress("Unable to proceed. IngestorInfo table and calls row counts should be matched.");

        //            throw new Exception("Unable to proceed. IngestorInfo table and calls row counts should be matched.");
        //        }
        //    }
        //    else
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        sb.Append("CREATE TABLE [dbo].[IngestorInfo1]([id] [int] IDENTITY(1,1) NOT NULL,[call_id] [uniqueidentifier] NOT NULL,[channel_key] [nvarchar](max) NULL,[call_type] [smallint] NOT NULL,[start_datetime] [datetimeoffset](7) NOT NULL,[stop_datetime] [datetimeoffset](7) NOT NULL,[DataIngestStatus] [int] NOT NULL,[SyncedToElastic] [bit] NOT NULL, CONSTRAINT [PK_IngestorInfo] PRIMARY KEY CLUSTERED ([id] ASC)) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];");
        //        sb.Append("ALTER TABLE [dbo].[IngestorInfo] ADD  DEFAULT ((0)) FOR [DataIngestStatus];");
        //        sb.Append("ALTER TABLE [dbo].[IngestorInfo] ADD  DEFAULT (CONVERT([bit],(0))) FOR [SyncedToElastic];");
        //        await _sourceDbContext.ExecuteRawSql(sb.ToString());

        //        _notifyProgress("IngestorInfo Table is created in source database.");

        //        StringBuilder sp = new StringBuilder();
        //        sp.Append("IF OBJECT_ID('[dbo].[dm_IngestCallSummaryToIngestorInfo]', 'P') IS NOT NULL BEGIN DROP PROCEDURE [dbo].[dm_IngestCallSummaryToIngestorInfo] END;");
        //        sp.Append("CREATE PROCEDURE [dbo].[dm_IngestCallSummaryToIngestorInfo] @OffsetValue INT = 1000, @NextRows INT = 10000 AS BEGIN SET NOCOUNT ON; INSERT INTO [dbo].[IngestorInfo]( [call_id] , [channel_key] , [call_type] , [start_datetime] , [stop_datetime]) SELECT Id, [channel_key] , [call_type] , [start_datetime] , [stop_datetime] FROM ww.calls ORDER BY(SELECT NULL) OFFSET @OffsetValue ROWS FETCH NEXT @NextRows ROWS ONLY END;");
        //        await _sourceDbContext.ExecuteRawSql(sp.ToString());

        //        _notifyProgress("IngestorInfo data dump procedure created in source database.");
        //    }

        //    return new IngestTableInfor { TotalRowCount = ingestorTableRowCount };
        //}


        private async Task<SourceTableInfo> GetTargetDataSummaryAsync(Action<ProgressNotifier> notifyProgress)
        {
            var totalCalls = await _targetDbContext.TableRowCountAsync<Call>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading target call tables data information.", Field = UIFields.TargetIngestedCallCount, FieldValue = totalCalls });

            var totalMediaStubs = await _targetDbContext.TableRowCountAsync<MediaStub>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading target media stubs tables data information.", Field = UIFields.TargetIngestedMediaStubCount, FieldValue = totalMediaStubs });

            var totalVoxStubs = await _targetDbContext.TableRowCountAsync<VoxStub>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading source vox stubs tables data information.", Field = UIFields.TargetIngestedVoxStubCount, FieldValue = totalVoxStubs });

            return new SourceTableInfo
            {
                TotalCalls = totalCalls,
                TotalMediaStubs = totalMediaStubs,
                TotalVoxStubs = totalVoxStubs
            };
        }

        private async Task<SourceTableInfo> GetSourceDataSummaryAsync(Action<ProgressNotifier> notifyProgress)
        {
            var totalCalls = await _sourceDbContext.TableRowCountAsync<Call>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading source call tables data information.", Field = UIFields.SourceCallCount, FieldValue = totalCalls });

            var totalMediaStubs = await _sourceDbContext.TableRowCountAsync<MediaStub>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading source media stubs tables data information.", Field = UIFields.SourceMediaStubCount, FieldValue = totalMediaStubs });

            var totalVoxStubs = await _sourceDbContext.TableRowCountAsync<VoxStub>();
            notifyProgress(new ProgressNotifier { Message = $"Completed - Loading source vox stubs tables data information.", Field = UIFields.SourceVoxStubCount, FieldValue = totalVoxStubs });

            notifyProgress(new ProgressNotifier { Message = "Completed - Loading source tables data information." });

            // call distribution
            var callMinDate = await _sourceDbContext.Calls.MinAsync(x => x.start_datetime);
            notifyProgress(new ProgressNotifier { Field = UIFields.CallsMinDate, FieldValue = callMinDate });

            var callMaxDate = await _sourceDbContext.Calls.MaxAsync(x => x.start_datetime);
            notifyProgress(new ProgressNotifier { Field = UIFields.CallsMaxDate, FieldValue = callMaxDate });

            notifyProgress(new ProgressNotifier { Field = UIFields.SourceCallDistribution, FieldValue = Math.Round((callMaxDate - callMinDate).TotalDays) });

            return new SourceTableInfo
            {
                TotalCalls = totalCalls,
                TotalMediaStubs = totalMediaStubs,
                TotalVoxStubs = totalVoxStubs
            };
        }

        //private async Task IngestCallsToInforTableAsync(SourceTableInfo _sourceTableInfo, IngestTableInfor _ingestTableInfor)
        //{
        //    if (_ingestTableInfor.TotalRowCount > 0 && _sourceTableInfo.TotalCalls == _ingestTableInfor.TotalRowCount)
        //        return;

        //    int totalPages = _sourceTableInfo.TotalCalls / _applicationSettings.QueringBatchSize;
        //    int pageNumber = 0;

        //    do
        //    {
        //        var param = new SqlParameter[]
        //        {
        //            new SqlParameter() { ParameterName = "@OffsetValue", SqlDbType = System.Data.SqlDbType.Int, Size = 100, Direction = System.Data.ParameterDirection.Input, Value = pageNumber },
        //            new SqlParameter() { ParameterName = " @NextRows", SqlDbType = System.Data.SqlDbType.Int, Direction = System.Data.ParameterDirection.Input, Value = totalPages}
        //        };

        //        await _sourceDbContext.ExecuteRawSql("EXEC [dbo].[dm_IngestCallSummaryToIngestorInfo] @OffsetValue, @NextRows", param);

        //        _notifyProgress($"{pageNumber}/{totalPages} Inserted call snapshot to IngestorInfo table.");

        //        pageNumber++;

        //    } while (pageNumber <= totalPages);

        //    _notifyProgress("Completed - Inserting call snapshot to IngestorInfo table.");
        //}

        private async Task<List<SyncedTableInfo>> GetTableSyncedInfoAsync(Action<ProgressNotifier> notifyProgress)
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

            using var context = new SourceDbContext(_applicationSettings);
            return await ((ISourceDbContext)context).SyncedTableInfo.ToListAsync();

            //var result = await ((ISourceDbContext)_sourceDbContext).SyncedTableInfo.ToListAsync();
            //return result;
        }

        public async Task<SourceDbTableInfo> IngestDataToInforTableAsync(Action<ProgressNotifier> notifyProgress)
        {
            if (_sourceTableInfo.TotalCalls == 0)
            {
                _sourceTableInfo = await GetSourceDataSummaryAsync(notifyProgress);
                _targetTableInfo = await GetTargetDataSummaryAsync(notifyProgress);

            }
            //_ingestTableInfor = await InitIngestorInforTable(); this is an alternative approach to track tables using call ids
            // await IngestCallsToInforTableAsync(_sourceTableInfo, _ingestTableInfor);

            _syncedTableInfo = await GetTableSyncedInfoAsync(notifyProgress);


            return new SourceDbTableInfo
            {
                SourceTableInfo = _sourceTableInfo,
                IngestTableInfor = _ingestTableInfor,
                SyncedTableInfo = _syncedTableInfo,
                TargetTableInfo = _targetTableInfo
            };
        }
    }
}
