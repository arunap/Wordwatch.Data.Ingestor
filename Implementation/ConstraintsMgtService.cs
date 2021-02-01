using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Infrastructure;

namespace Wordwatch.Data.Ingestor.Implementation
{
    public sealed class ConstraintsMgtService
    {
        private readonly SourceDbContext _sourceDbContext;
        private readonly TargetDbContext _targetDbContext;
        private readonly ApplicationSettings _applicationSettings;

        public ConstraintsMgtService(SourceDbContext sourceDbContext, TargetDbContext targetDbContext, IOptions<ApplicationSettings> applicationSettings)
        {
            _sourceDbContext = sourceDbContext;
            _targetDbContext = targetDbContext;
            _applicationSettings = applicationSettings?.Value;
        }

        public async Task BuildIndexesAsync(DbContextType contextType, IProgress<ProgressNotifier> progress)
        {
            var sqlQueries = contextType == DbContextType.Target ? _applicationSettings.BackendSettings.TargetIdxToBuild : _applicationSettings.BackendSettings.SourceIdxToBuild;

            foreach (var item in sqlQueries)
            {
                progress.Report(new ProgressNotifier { Message = $"Building {contextType} Index: {item}" });

                if (contextType == DbContextType.Source)
                    await _sourceDbContext.ExecuteRawSql(item);
                else
                    await _targetDbContext.ExecuteRawSql(item);

                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Building {contextType} Index: {item}" });
            }
        }

        public async Task UpdateNonClusteredIdxAsync(IdxConstMgtStatus indexManageStatus, IProgress<ProgressNotifier> progress, DbContextType contextType = DbContextType.Target)
        {
            List<Application.Models.TableIndex> indexes;
            if (contextType == DbContextType.Target)
                indexes = await _targetDbContext.TableIndexes.FromSqlRaw(_applicationSettings.BackendSettings.NonClusteredIdxStatusQuery).ToListAsync();
            else
                throw new NotImplementedException("Not implemented for source non-clustereded");

            if (indexManageStatus == IdxConstMgtStatus.Disable)
            {
                foreach (var item in indexes.Where(x => x.IsDisabled == false))
                {
                    progress.Report(new ProgressNotifier { Message = $"{indexManageStatus} TARGET Index: {item.DisableQuery}" });
                    await _targetDbContext.ExecuteRawSql(item.DisableQuery);
                    progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - {indexManageStatus} TARGET Index: {item.DisableQuery}" });
                }
            }
            else
            {
                foreach (var item in indexes.Where(x => x.IsDisabled == true))
                {
                    progress.Report(new ProgressNotifier { Message = $"{indexManageStatus} TARGET Index: {item.EnableQuery}" });
                    await _targetDbContext.ExecuteRawSql(item.EnableQuery);
                    progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - {indexManageStatus} TARGET Index: {item.EnableQuery}" });
                }
            }
        }

        public async Task UpdateFKConstraintsAsync(IdxConstMgtStatus indexManageStatus, IProgress<ProgressNotifier> progress)
        {
            var sqlQueries = indexManageStatus == IdxConstMgtStatus.Disable ? _applicationSettings.BackendSettings.FKConstraintsDisableQuery : _applicationSettings.BackendSettings.FKConstraintsEnableQuery;
            foreach (var item in sqlQueries)
            {
                progress.Report(new ProgressNotifier { Message = $"{indexManageStatus} TARGET Constraint: {item}" });
                await _targetDbContext.ExecuteRawSql(item);
                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - {indexManageStatus} TARGET Constraint: {item}" });
            }
        }

        public async Task SetDefaultConstraints(IProgress<ProgressNotifier> progress)
        {
            var sqlQueries = _applicationSettings.BackendSettings.TargetDefaultConstraints;

            foreach (var item in sqlQueries)
            {
                progress.Report(new ProgressNotifier { Message = $"TARGET Constraint: {item}" });
                await _targetDbContext.ExecuteRawSql(item);
                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - TARGET Constraint: {item}" });
            }
        }
    }
}
