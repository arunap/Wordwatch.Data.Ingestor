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
    public class MigrationActionService : IMigrationActionService
    {
        private readonly ILogger<MigrationActionService> _logger;
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ApplicationSettings _applicationSettings;
        private readonly SystemInitializerService _systemInitializer;
        private readonly InsertTableRowsService _insertTableRowsService;
        private bool _pausedClicked = false;

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
            await _systemInitializer.InitTableAsync(_migrationProgress);
        }

        public async Task Pause(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            _pausedClicked = true;

            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Pause Action." });

            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Pause Operations." });

            await Task.CompletedTask;
        }

        public async Task ResumeAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Resume Action." });
           
            await StartAync(notifyProgress, cancellationToken);
        }

        public async Task StartAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            int loopCount = await GetPendingIterationCountAsync();

            int idx = 0;

            while (idx <= loopCount && !_pausedClicked)
            {
                await _insertTableRowsService.InitAsync(SyncTableNames.CallsTable, notifyProgress);
              //  await _insertTableRowsService.InitAsync(SyncTableNames.MediaStubsTable, notifyProgress);
              //  await _insertTableRowsService.InitAsync(SyncTableNames.VoxStubsTable, notifyProgress);

                idx++;
            }

            if (_pausedClicked)
            {
                notifyProgress.Report(new ProgressNotifier { Message = $"Migrations are successfully paused!." });
            }
        }

        public async Task StopAync(IProgress<ProgressNotifier> notifyProgress, CancellationToken cancellationToken)
        {
            notifyProgress.Report(new ProgressNotifier { Message = $"User Clicked Stop Action." });

            _pausedClicked = true;
            notifyProgress.Report(new ProgressNotifier { Message = $"Please wait until the system finalizing Stop Operations." });

            // Enable indexes.

            await Task.CompletedTask;
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
