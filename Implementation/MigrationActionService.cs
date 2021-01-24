using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public class DataIngestStatusEvent : EventArgs
    {
        public IProgress<ProgressNotifier> Progress { get; set; }
    }

    public class MigrationActionService : IMigrationActionService
    {
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ILogger<MigrationActionService> _logger;
        private readonly ApplicationSettings _applicationSettings;
        private readonly SystemInitializerService _systemInitializer;
        private readonly InsertTableRowsService _insertTableRowsService;
        private DataIngestStatus _dataIngestStatus = DataIngestStatus.Pending;

        public MigrationActionService(
            ILogger<MigrationActionService> logger,
            SourceDbContext sourceDbContext, TargetDbContext targetDbContext,
            IOptions<ApplicationSettings> applicationSettings, SystemInitializerService systemInitializer,
            InsertTableRowsService insertTableRowsService)
        {
            _logger = logger;
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings.Value;
            _systemInitializer = systemInitializer;
            _insertTableRowsService = insertTableRowsService;
        }

        public async Task InitAsync(IProgress<ProgressNotifier> _migrationProgress, CancellationToken cancellationToken)
        {
            _dataIngestStatus = DataIngestStatus.Pending;
            _migrationProgress.Report(new ProgressNotifier { Message = $"Initialising Source & Target Database connections!!!" });

            await _systemInitializer.InitTableAsync(_migrationProgress);

            await _targetDbContext.DisableNonClusteredIndexAsync(_migrationProgress);
            await _targetDbContext.DisableConstraints(_migrationProgress);

            await _sourceDbContext.BuildIndexesAsync(_migrationProgress);

            _migrationProgress.Report(new ProgressNotifier { Message = "Source & Target Databases are READY for Migrations." });
        }

        public async Task Pause(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Pause Action." });
            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Pause Operations." });

            _dataIngestStatus = DataIngestStatus.Paused;

            await Task.CompletedTask;
        }

        public async Task ResumeAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Resume Action." });

            _dataIngestStatus = DataIngestStatus.Resumed;

            await StartAync(notifyProgress, cancellationToken);
        }

        public async Task StartAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            _dataIngestStatus = DataIngestStatus.Started;

            int loopCount = await GetPendingIterationCountAsync();

            int idx = 0;

            while (idx <= loopCount && _dataIngestStatus != DataIngestStatus.Paused && _dataIngestStatus != DataIngestStatus.Stopped)
            {
                await _insertTableRowsService.InsertRowsAsync(SyncTableNames.CallsTable, notifyProgress);
                await _insertTableRowsService.InsertRowsAsync(SyncTableNames.MediaStubsTable, notifyProgress);
                await _insertTableRowsService.InsertRowsAsync(SyncTableNames.VoxStubsTable, notifyProgress);

                idx++;
            }

            if (idx >= loopCount)
            {
                _dataIngestStatus = DataIngestStatus.Completed;
            }

            if (_dataIngestStatus == DataIngestStatus.Completed || _dataIngestStatus == DataIngestStatus.Stopped)
            {
                await EnableConstraintsAndNonClusteredIndexAsync(notifyProgress);
                _dataIngestStatus = DataIngestStatus.Finished;
            }

            if (_dataIngestStatus == DataIngestStatus.Paused)
            {
                notifyProgress.Report(new ProgressNotifier { Message = $"Data Migrations successfully {DataIngestStatus.Paused}!." });
            }
        }

        public async Task StopAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Stop Action." });
            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Stop Operations." });

            if (_dataIngestStatus == DataIngestStatus.Pending || _dataIngestStatus == DataIngestStatus.Paused)
            {
                while (_dataIngestStatus != DataIngestStatus.Pending && _dataIngestStatus != DataIngestStatus.Paused)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    notifyProgress.Report(new ProgressNotifier { Message = $"Waiting on Stop Operations..." });
                }
                await EnableConstraintsAndNonClusteredIndexAsync(notifyProgress);
                _dataIngestStatus = DataIngestStatus.Finished;
            }
            else if (_dataIngestStatus != DataIngestStatus.Finished)
            {
                _dataIngestStatus = DataIngestStatus.Stopped;

                while (_dataIngestStatus != DataIngestStatus.Finished)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    notifyProgress.Report(new ProgressNotifier { Message = $"Waiting on Stop Operations..." });
                }

                notifyProgress.Report(new ProgressNotifier { Message = $"Data Migrations successfully {_dataIngestStatus}!." });
            }

            await Task.CompletedTask;
        }

        private async Task EnableConstraintsAndNonClusteredIndexAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            await _targetDbContext.EnableConstraints(notifyProgress);
            await _targetDbContext.EnableNonClusteredIndexAsync(notifyProgress);
        }

        private async Task<int> GetPendingIterationCountAsync()
        {
            var sourceCallMaxDate = await _sourceDbContext.Calls.MaxAsync(x => x.start_datetime);
            var targetMaxSyncedDate = await _sourceDbContext.SyncedTableInfo.Where(x => x.RelatedTable == SyncTableNames.CallsTable).Select(d => d.LastSyncedAt).FirstAsync();

            double rowCount = 0;

            if (targetMaxSyncedDate == null || targetMaxSyncedDate.Value == DateTimeOffset.MinValue)
            {
                var sourceCallMinDate = await _sourceDbContext.Calls.MinAsync(x => x.start_datetime);

                rowCount = Math.Round((sourceCallMaxDate - sourceCallMinDate).TotalDays);
            }
            else
            {
                rowCount = Math.Round((sourceCallMaxDate - targetMaxSyncedDate.Value).TotalDays);
            }

            return int.Parse(rowCount.ToString());
        }
    }
}
