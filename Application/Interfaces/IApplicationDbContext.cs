using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wordwatch.Data.Ingestor.Domain.Entities;

namespace Wordwatch.Data.Ingestor.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Call> Calls { get; set; }

        DbSet<MediaStub> MediaStubs { get; set; }

        DbSet<VoxStub> VoxStubs { get; set; }

        Task BatchInsertAsync<T>(List<T> entityList, CancellationToken cancellationToken = default) where T : class;

        Task<int> BatchUpdateAsync<T>(List<T> entityList, CancellationToken cancellationToken = default);

        Task<int> BatchDeleteAsync<T>(List<T> entityList, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> BatchReadAsync<TEntity>(
                Expression<Func<TEntity, bool>> filter = null,
                Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
                string includeProperties = "", int pageNumber = -1, int pageSize = 1000, CancellationToken cancellationToken = default) where TEntity : class;

        Task<bool> DataExistsAsync<T>() where T : class;

        Task<bool> TableExistsAsync<T>() where T : class;

        Task<int> TableRowCountAsync<T>() where T : class;
    }

    public interface ISourceDbContext : IApplicationDbContext { DbSet<IngestorInfo> IngestorInfos { get; set; } Task EnsureMigrationAsync(CancellationToken cancellationToken); }

    public interface ITargetDbContext : IApplicationDbContext { }
}
