﻿using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Application.Interfaces;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Infrastructure
{
    public abstract class ApplicationDbContext : DbContext, IApplicationDbContext
    { 
        public DbSet<Call> Calls { get; set; }
        public DbSet<MediaStub> MediaStubs { get; set; }
        public DbSet<VoxStub> VoxStubs { get; set; }

        public Task<int> BatchDeleteAsync<T>(List<T> entityList, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task BatchInsertAsync<T>(List<T> entityList, CancellationToken cancellationToken) where T : class
        {
            await this.BulkInsertAsync<T>(entityList, bulkConfig: new BulkConfig { BatchSize = 2000 }, progress: null, cancellationToken: cancellationToken);
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

        public async Task<bool> TableExistsAsync<T>() where T : class
        {
            DbSet<T> dbSet = this.Set<T>();
            return await dbSet.AnyAsync();
        }

        public async Task<int> TableRowCountAsync<T>() where T : class
        {
            DbSet<T> dbSet = this.Set<T>();
            return await dbSet.CountAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Call>().ToTable("calls");
            modelBuilder.Entity<MediaStub>().ToTable("media_stubs");
            modelBuilder.Entity<VoxStub>().ToTable("vox_stubs");

            //modelBuilder.Entity<Call>().Property(x => x.user_id).HasColumnType("UniqueIdentifier");

            modelBuilder.HasDefaultSchema("ww");
            base.OnModelCreating(modelBuilder);
        }
    }
}
