using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public DataIngestStatus DataIngestStatus { get; set; }
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
        private readonly IServiceProvider _serviceProvider;
        private MigrationSummary _migrationSummary;

        public event EventHandler<DataIngestStatusEvent> WorkflowStateChanged;

        public MigrationActionService(
            ILogger<MigrationActionService> logger,
            SourceDbContext sourceDbContext, TargetDbContext targetDbContext,
            IOptions<ApplicationSettings> applicationSettings, SystemInitializerService systemInitializer,
            InsertTableRowsService insertTableRowsService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings.Value;
            _systemInitializer = systemInitializer;
            _insertTableRowsService = insertTableRowsService;
            _serviceProvider = serviceProvider;
        }

        public async Task InitAsync(IProgress<ProgressNotifier> _migrationProgress, CancellationToken cancellationToken)
        {
            _dataIngestStatus = DataIngestStatus.Pending;
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Pending });

            _migrationSummary = await _systemInitializer.InitTableAsync(_migrationProgress);

            await _targetDbContext.DisableNonClusteredIndexAsync(_migrationProgress);
            await _targetDbContext.DisableConstraints(_migrationProgress);

            await _sourceDbContext.BuildIndexesAsync(_migrationProgress);

            _migrationProgress.Report(new ProgressNotifier { Message = "Source & Target Databases are READY for Migrations." });
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Ready });
        }

        public async Task Pause(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Paused });

            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Pause Action." });
            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Pause Operations." });

            _dataIngestStatus = DataIngestStatus.Paused;

            await Task.CompletedTask;
        }

        public async Task ResumeAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Resumed });

            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Resume Action." });

            _dataIngestStatus = DataIngestStatus.Resumed;

            await StartAync(notifyProgress, cancellationToken);
        }

        public async Task StartAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            _dataIngestStatus = DataIngestStatus.Started;
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Started });

            int loopCount = await GetPendingIterationCountAsync();

            int idx = 0;

            while (idx <= loopCount && _dataIngestStatus != DataIngestStatus.Paused && _dataIngestStatus != DataIngestStatus.Stopped)
            {
                List<Task> tasks = new List<Task>();

                using (var scope = _serviceProvider.CreateScope())
                {
                    InsertTableRowsService s1 = scope.ServiceProvider.GetRequiredService<InsertTableRowsService>();
                    tasks.Add(s1.InsertRowsAsync(SyncTableNames.CallsTable, notifyProgress));

                    if (_migrationSummary.SourceTableInfo.TotalVoxStubs > 0)
                    {
                        InsertTableRowsService s2 = scope.ServiceProvider.GetRequiredService<InsertTableRowsService>();
                        tasks.Add(s2.InsertRowsAsync(SyncTableNames.VoxStubsTable, notifyProgress));
                    }

                    if (_migrationSummary.SourceTableInfo.TotalMediaStubs > 0)
                    {
                        InsertTableRowsService s3 = scope.ServiceProvider.GetRequiredService<InsertTableRowsService>();
                        tasks.Add(s3.InsertRowsAsync(SyncTableNames.MediaStubsTable, notifyProgress));
                    }
                    await Task.WhenAll(tasks);
                }

                idx++;
            }

            if (idx >= loopCount)
            {
                _dataIngestStatus = DataIngestStatus.Completed;
                WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Completed });
            }

            if (_dataIngestStatus == DataIngestStatus.Completed || _dataIngestStatus == DataIngestStatus.Stopped)
            {
                await _targetDbContext.EnableConstraints(notifyProgress);
                await _targetDbContext.EnableNonClusteredIndexAsync(notifyProgress);

                _dataIngestStatus = DataIngestStatus.Finished;
            }

            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = _dataIngestStatus });
            notifyProgress.Report(new ProgressNotifier { Message = $"Data Migrations successfully {_dataIngestStatus}!." });
        }

        public async Task StopAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"Performing Stop Action." });
            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Stop Operations." });

            if (_dataIngestStatus != DataIngestStatus.Finished)
            {
                _dataIngestStatus = DataIngestStatus.Stopped;
                WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Stopped });
            }

            await Task.CompletedTask;
        }

        public async Task ExitAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken = default)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"Performing Exit Action." });
            await StopAync(notifyProgress, cancellationToken);
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
