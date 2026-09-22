using Microsoft.EntityFrameworkCore.Storage;

namespace CleanBase.Application.Common.Abstractions;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>()
        where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();

    // Transaction support
    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    );

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}