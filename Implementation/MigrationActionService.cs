using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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

        private List<SyncedTableInfo> _syncedTableInfo;

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
            _syncedTableInfo = await _systemInitializer.InitTableAsync(_migrationProgress);
        }

        public Task Pause(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ResumeAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StartAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken)
        {
            await _insertTableRowsService.InitAsync(SyncTableNames.CallsTable, progress);
        }

        public Task StopAync(IProgress<ProgressNotifier> progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
