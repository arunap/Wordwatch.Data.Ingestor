using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Models;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public abstract class ApplicationDbContext : DbContext
    {
        private readonly ApplicationSettings _applicationSettings;
        protected ApplicationDbContext(IOptions<ApplicationSettings> applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;
        }

        public DbSet<Call> Calls { get; set; }
        public DbSet<MediaStub> MediaStubs { get; set; }
        public DbSet<VoxStub> VoxStubs { get; set; }

        public Task<int> BatchDeleteAsync<T>(List<T> entityList, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task BatchInsertAsync<T>(List<T> entityList, CancellationToken cancellationToken) where T : class
        {
            await this.BulkInsertAsync<T>(entityList, bulkConfig: new BulkConfig { BatchSize = _applicationSettings.IngestBatchSize }, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> BatchReadAsync<TEntity>(
                Expression<Func<TEntity, bool>> filter = null,
                Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                string includeProperties = "", int pageNumber = -1, int pageSize = 1000, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbSet<TEntity> dbSet = this.Set<TEntity>();
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (pageNumber != -1)
            {
                if (pageNumber == 1)
                    query = query.Take(pageSize);
                else
                    query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public async Task<int> BatchUpdateAsync<T>(List<T> entityList, CancellationToken cancellationToken)
        {
            int result;
            using (var transaction = this.Database.BeginTransaction())
            {
                result = await this.BatchUpdateAsync(entityList, cancellationToken: cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }

        public async Task<bool> DataExistsAsync<T>() where T : class
        {
            DbSet<T> dbSet = this.Set<T>();
            return await dbSet.AnyAsync();
        }

        public async Task<bool> TableExistsAsync<T>(string schema = "dbo") where T : class
        {
            bool exists = false;
            var conn = Database.GetDbConnection();
            if (conn.State.Equals(System.Data.ConnectionState.Closed))
                await conn.OpenAsync();

            using (var command = conn.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{typeof(T).Name}'";
                exists = await command.ExecuteScalarAsync() != null;
            }

            return exists;
        }

        public async Task<int> TableRowCountByIdAsync<T>() where T : class
        {
            var conn = Database.GetDbConnection();
            if (conn.State.Equals(System.Data.ConnectionState.Closed))
                await conn.OpenAsync();

            int rowcount = 0;
            using (var command = conn.CreateCommand())
            {
                command.CommandTimeout = _applicationSettings.CommandTimeout;

                var entityType = this.Model.FindEntityType(typeof(T));
                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                command.CommandText = $"SELECT COUNT(Id) FROM {schema}.{tableName}";
                var scalarVal = await command.ExecuteScalarAsync();

                int.TryParse(scalarVal.ToString(), out rowcount);
            }

            return rowcount;
        }

        public async Task ExecuteRawSql(string sql, SqlParameter[] parameters = null, CancellationToken cancellationToken = default)
        {
            if (parameters == null) parameters = new SqlParameter[] { };

            await this.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Call>().ToTable("calls");
            modelBuilder.Entity<MediaStub>().ToTable("media_stubs");
            modelBuilder.Entity<VoxStub>().ToTable("vox_stubs");

            modelBuilder.HasDefaultSchema("ww");

            base.OnModelCreating(modelBuilder);
        }
    }
}
