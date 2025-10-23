using System.Data;
using BuildingBlocks.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Infrastructure.Data;

/// <summary>
/// Implementação do padrão Unit of Work para o AuthService
/// Gerencia transações e operações de persistência usando Entity Framework Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AuthDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed = false;

    public UnitOfWork(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Salva todas as mudanças pendentes no contexto
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log do erro se necessário
            throw new InvalidOperationException("Erro ao salvar mudanças no banco de dados", ex);
        }
    }

    /// <summary>
    /// Inicia uma nova transação
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("Uma transação já está ativa");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Confirma a transação atual
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("Nenhuma transação ativa para confirmar");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Desfaz a transação atual
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("Nenhuma transação ativa para desfazer");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Libera os recursos da transação atual
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Libera todos os recursos utilizados
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _currentTransaction?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }
}