using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Constants;
using Wordwatch.Data.Ingestor.Application.Models;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public static class DbCustomExtentions
    {
        private const string sqlNonClusteredIndexQuery = "SELECT *, CONCAT('ALTER INDEX ', IndexName, ' ON ','ww.',TableName, ' REBUILD;') AS EnableQuery , CONCAT('ALTER INDEX ', IndexName, ' ON ','ww.',TableName, ' DISABLE;') AS DisableQuery FROM (SELECT OBJECT_NAME(OBJECT_ID) as TableName, [name] AS IndexName FROM sys.indexes WHERE TYPE_DESC = 'NONCLUSTERED' AND ( OBJECT_NAME(OBJECT_ID) IN ('calls', 'media_stubs', 'vox_stubs'))) R";

        public static async Task EnableNonClusteredIndexAsync(this TargetDbContext targetDbContext, IProgress<ProgressNotifier> progress)
        {
            List<Application.Models.TableIndex> indexes = await targetDbContext.TableIndexes.FromSqlRaw(sqlNonClusteredIndexQuery).ToListAsync();
            foreach (var item in indexes)
            {
                progress.Report(new ProgressNotifier { Message = $"Enabling TARGET Index: {item.EnableQuery}" });
                await targetDbContext.ExecuteRawSql(item.EnableQuery);
                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Enabling TARGET Index: {item.EnableQuery}" });
            }
        }

        public static async Task DisableNonClusteredIndexAsync(this TargetDbContext targetDbContext, IProgress<ProgressNotifier> progress)
        {
            List<Application.Models.TableIndex> indexes = await targetDbContext.TableIndexes.FromSqlRaw(sqlNonClusteredIndexQuery).ToListAsync();
            foreach (var item in indexes)
            {
                progress.Report(new ProgressNotifier { Message = $"Disabling TARGET Index: {item.DisableQuery}" });
                await targetDbContext.ExecuteRawSql(item.EnableQuery);
                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Disabling TARGET Index: {item.DisableQuery}" });
            }
        }

        public static async Task BuildIndexesAsync(this SourceDbContext sourceDbContext, IProgress<ProgressNotifier> progress)
        {
            string[] indexes = new string[] { "ALTER INDEX IX_calls_strt_dttm ON ww.calls REBUILD;", "ALTER INDEX idx_vox_stubs_start_datetime ON ww.vox_stubs REBUILD;" };
            foreach (var item in indexes)
            {
                progress.Report(new ProgressNotifier { Message = $"Building SOURCE Index: {item}" });
                await sourceDbContext.ExecuteRawSql(item);
                progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Building SOURCE Index: {item}" });
            }
        }

        public static async Task DisableConstraints(this TargetDbContext targetDbContext, IProgress<ProgressNotifier> progress)
        {
            string sql = "ALTER TABLE [ww].[media_stubs] NOCHECK CONSTRAINT [FK_media_stubs_calls];";
            progress.Report(new ProgressNotifier { Message = $"Disabling TARGET Constraints: {sql}" });
            await targetDbContext.ExecuteRawSql(sql);
            progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Disabling TARGET Constraints: {sql}." });
        }

        public static async Task EnableConstraints(this TargetDbContext targetDbContext, IProgress<ProgressNotifier> progress)
        {
            string sql = "ALTER TABLE [ww].[media_stubs] CHECK CONSTRAINT [FK_media_stubs_calls];";
            progress.Report(new ProgressNotifier { Message = $"Enabling TARGET Constraints: {sql}" });
            await targetDbContext.ExecuteRawSql(sql);
            progress.Report(new ProgressNotifier { Message = $"{MigrationMessageActions.Completed} - Enabling TARGET Constraints: {sql} ." });
        }

        public static string GetConnectionDetails(this string connectionString)
        {
            using SqlConnection con = new SqlConnection(connectionString);
            return $"Server: {con.DataSource}, Database: {con.Database}";
        }
    }
}
