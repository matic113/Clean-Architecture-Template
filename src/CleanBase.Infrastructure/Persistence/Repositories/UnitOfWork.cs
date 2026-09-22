using System.Collections.Concurrent;
using CleanBase.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace CleanBase.Infrastructure.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    // ConcurrentDictionary for thread-safe repository caching
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _currentTransaction;

    public IRepository<T> Repository<T>()
        where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(context));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public int SaveChanges()
    {
        return context.SaveChanges();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_currentTransaction is not null)
            return _currentTransaction;

        _currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}