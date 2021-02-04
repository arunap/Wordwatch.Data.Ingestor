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
        private readonly ConstraintsMgtService _constraintsMgtService;
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ILogger<MigrationActionService> _logger;
        private readonly ApplicationSettings _applicationSettings;
        private readonly SystemInitializerService _systemInitializer;
        private DataIngestStatus _dataIngestStatus = DataIngestStatus.Pending;
        private readonly IServiceProvider _serviceProvider;
        private MigrationSummary _migrationSummary;

        public event EventHandler<DataIngestStatusEvent> WorkflowStateChanged;

        public MigrationActionService(
            ILogger<MigrationActionService> logger,
            SourceDbContext sourceDbContext, TargetDbContext targetDbContext,
            IOptions<ApplicationSettings> applicationSettings, SystemInitializerService systemInitializer,
            InsertTableRowsService insertTableRowsService, IServiceProvider serviceProvider, ConstraintsMgtService constraintsMgtService)
        {
            _logger = logger;
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings.Value;
            _systemInitializer = systemInitializer;
            _serviceProvider = serviceProvider;
            _constraintsMgtService = constraintsMgtService;
        }

        public async Task InitAsync(IProgress<ProgressNotifier> _migrationProgress, CancellationToken cancellationToken)
        {
            _dataIngestStatus = DataIngestStatus.Pending;
            WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Pending });

            _migrationSummary = await _systemInitializer.InitTableAsync(_migrationProgress);

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

            bool isFirstTimeSync = _migrationSummary.SyncedTableInfo.Where(t => t.RelatedTable == SyncTableNames.CallsTable).Select(x => x.LastSyncedAt).First() == null;

            if (isFirstTimeSync)
            {
                await _constraintsMgtService.SetDefaultConstraints(notifyProgress);
            }

            if (_applicationSettings.BackendSettings.SourcePKBuildRequired && isFirstTimeSync)
            {
                await _constraintsMgtService.BuildIndexesAsync(DbContextType.Source, notifyProgress);
            }

            if (_applicationSettings.BackendSettings.DisableConstraints)
            {
                await _constraintsMgtService.UpdateFKConstraintsAsync(IdxConstMgtStatus.Disable, notifyProgress);
                await _constraintsMgtService.UpdateNonClusteredIdxAsync(IdxConstMgtStatus.Disable, notifyProgress, DbContextType.Target);
            }

            if (!isFirstTimeSync && _applicationSettings.BackendSettings.TargetPKBuildRequired)
            {
                await _constraintsMgtService.BuildIndexesAsync(DbContextType.Target, notifyProgress);
            }

            int loopCount = await GetPendingIterationCountAsync(notifyProgress);

            int idx = 0;

            while (idx < loopCount && _dataIngestStatus != DataIngestStatus.Paused && _dataIngestStatus != DataIngestStatus.Stopped)
            {
                if (idx != 0 && _applicationSettings.BackendSettings.PKIndexBuildInterval != 0 && (idx % _applicationSettings.BackendSettings.PKIndexBuildInterval) == 0)
                {
                    notifyProgress.Report(new ProgressNotifier { Message = $"Reached to a Index build interval: {idx}" });
                    await _constraintsMgtService.BuildIndexesAsync(DbContextType.Target, notifyProgress);

                    var calls = _targetDbContext.TableRowCountByIdAsync<Call>();
                    var media = _targetDbContext.TableRowCountByIdAsync<MediaStub>();
                    var vox = _targetDbContext.TableRowCountByIdAsync<VoxStub>();

                    var results = await Task.WhenAll(calls, media, vox);
                    notifyProgress.Report(new ProgressNotifier { Message = $"Calls: {results[0]:N0}, Media Stubs: {results[1]:N0}, Vox Stubs: {results[2]:N0}" });
                }

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
                // WorkflowStateChanged?.Invoke(null, new DataIngestStatusEvent { DataIngestStatus = DataIngestStatus.Completed });
            }

            if (_dataIngestStatus == DataIngestStatus.Completed || _dataIngestStatus == DataIngestStatus.Stopped)
            {
                // await _constraintsMgtService.BuildIndexesAsync(DbContextType.Target, notifyProgress);
                await _constraintsMgtService.UpdateFKConstraintsAsync(IdxConstMgtStatus.Enable, notifyProgress);
                await _constraintsMgtService.UpdateNonClusteredIdxAsync(IdxConstMgtStatus.Enable, notifyProgress, DbContextType.Target);

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

        private async Task<int> GetPendingIterationCountAsync(IProgress<ProgressNotifier> notifyProgress)
        {
            notifyProgress.Report(new ProgressNotifier { Message = "Get next max iteration date time infomation. Wait..." });
            var tableInfo = await _sourceDbContext.SyncedTableInfo.Where(x => x.RelatedTable == SyncTableNames.CallsTable).FirstAsync();
            if (_applicationSettings.NoOfCallsToSync > 0)
            {
                DateTimeOffset valueToCompare;
                if (tableInfo.LastSyncedAt != null)
                    valueToCompare = new DateTimeOffset(tableInfo.LastSyncedAt.Value, DateTimeOffset.UtcNow.Offset);
                else
                    valueToCompare = new DateTimeOffset(tableInfo.MinDate.Value, DateTimeOffset.UtcNow.Offset);

                var nextDate = await _sourceDbContext.Calls.Where(x => x.start_datetime >= valueToCompare.AddDays(1))
                                    .Select(x => x.start_datetime)
                                    .OrderBy(x => x)
                                    .Take(_applicationSettings.NoOfCallsToSync)
                                    .MaxAsync();

                notifyProgress.Report(new ProgressNotifier { Message = $"Migration will run for next {_applicationSettings.NoOfCallsToSync:N0} calls, till: {nextDate:yyyy-MM-dd}" });
                return Convert.ToInt32((nextDate - valueToCompare).TotalDays);
            }

            notifyProgress.Report(new ProgressNotifier { Message = $"Application will run migration data till {tableInfo.MaxDate:yyyy-MM-dd}" });
            return tableInfo.DaysPending;
        }
    }
}
