using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Data;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório para a entidade RefreshToken
/// Utiliza Entity Framework Core para operações de banco de dados
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Construtor que recebe o contexto de banco via injeção de dependência
    /// </summary>
    /// <param name="context">Contexto de banco de dados</param>
    public RefreshTokenRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Busca um refresh token por ID
    /// </summary>
    /// <param name="id">ID do refresh token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Refresh token encontrado ou null</returns>
    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar refresh token por ID {id}", ex);
        }
    }

    /// <summary>
    /// Busca todos os refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de todos os refresh tokens</returns>
    public async Task<IReadOnlyList<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshTokens = await _context.RefreshTokens
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            return refreshTokens.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar todos os refresh tokens", ex);
        }
    }

    /// <summary>
    /// Busca refresh tokens que atendem ao predicado especificado
    /// </summary>
    /// <param name="predicate">Expressão de filtro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de refresh tokens que atendem ao filtro</returns>
    public async Task<IReadOnlyList<RefreshToken>> FindAsync(Expression<Func<RefreshToken, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var refreshTokens = await _context.RefreshTokens
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
            
            return refreshTokens.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar refresh tokens com filtro", ex);
        }
    }

    /// <summary>
    /// Adiciona um novo refresh token
    /// </summary>
    /// <param name="entity">Refresh token a ser adicionado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Refresh token adicionado</returns>
    public async Task<RefreshToken> AddAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = await _context.RefreshTokens.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao adicionar refresh token", ex);
        }
    }

    /// <summary>
    /// Atualiza um refresh token existente
    /// </summary>
    /// <param name="entity">Refresh token a ser atualizado</param>
    public void Update(RefreshToken entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.RefreshTokens.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao atualizar refresh token", ex);
        }
    }

    /// <summary>
    /// Remove um refresh token
    /// </summary>
    /// <param name="entity">Refresh token a ser removido</param>
    public void Remove(RefreshToken entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.RefreshTokens.Remove(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao remover refresh token", ex);
        }
    }
}