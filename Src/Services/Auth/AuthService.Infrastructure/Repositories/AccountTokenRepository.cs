using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Data;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório para a entidade AccountToken
/// Utiliza Entity Framework Core para operações de banco de dados
/// </summary>
public class AccountTokenRepository : IAccountTokenRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Construtor que recebe o contexto de banco via injeção de dependência
    /// </summary>
    /// <param name="context">Contexto de banco de dados</param>
    public AccountTokenRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Busca um token de conta por ID
    /// </summary>
    /// <param name="id">ID do token de conta</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de conta encontrado ou null</returns>
    public async Task<AccountToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.AccountTokens
                .FirstOrDefaultAsync(at => at.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar token de conta por ID {id}", ex);
        }
    }

    /// <summary>
    /// Busca todos os tokens de conta
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de todos os tokens de conta</returns>
    public async Task<IReadOnlyList<AccountToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accountTokens = await _context.AccountTokens
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            return accountTokens.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar todos os tokens de conta", ex);
        }
    }

    /// <summary>
    /// Busca tokens de conta que atendem ao predicado especificado
    /// </summary>
    /// <param name="predicate">Expressão de filtro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de tokens de conta que atendem ao filtro</returns>
    public async Task<IReadOnlyList<AccountToken>> FindAsync(Expression<Func<AccountToken, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var accountTokens = await _context.AccountTokens
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
            
            return accountTokens.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar tokens de conta com filtro", ex);
        }
    }

    /// <summary>
    /// Adiciona um novo token de conta
    /// </summary>
    /// <param name="entity">Token de conta a ser adicionado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de conta adicionado</returns>
    public async Task<AccountToken> AddAsync(AccountToken entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = await _context.AccountTokens.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao adicionar token de conta", ex);
        }
    }

    /// <summary>
    /// Atualiza um token de conta existente
    /// </summary>
    /// <param name="entity">Token de conta a ser atualizado</param>
    public void Update(AccountToken entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.AccountTokens.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao atualizar token de conta", ex);
        }
    }

    /// <summary>
    /// Remove um token de conta
    /// </summary>
    /// <param name="entity">Token de conta a ser removido</param>
    public void Remove(AccountToken entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.AccountTokens.Remove(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao remover token de conta", ex);
        }
    }

    /// <summary>
    /// Busca um token de conta pelo valor do token
    /// </summary>
    /// <param name="token">Valor do token a ser buscado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de conta encontrado ou null</returns>
    public async Task<AccountToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token não pode ser nulo ou vazio", nameof(token));

            return await _context.AccountTokens
                .FirstOrDefaultAsync(at => at.Token == token, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar token de conta pelo valor {token}", ex);
        }
    }

    /// <summary>
    /// Busca todos os tokens ativos de um usuário por tipo
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="tokenType">Tipo do token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de tokens ativos do usuário</returns>
    public async Task<IReadOnlyList<AccountToken>> GetActiveTokensByUserIdAsync(string userId, AccountTokenType tokenType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("ID do usuário não pode ser nulo ou vazio", nameof(userId));

            var activeTokens = await _context.AccountTokens
                .AsNoTracking()
                .Where(at => at.UserId == userId && 
                           at.TokenType == tokenType &&
                           at.UsedAt == null &&
                           at.RevokedAt == null &&
                           at.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
            
            return activeTokens.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar tokens ativos do usuário {userId} do tipo {tokenType}", ex);
        }
    }

    /// <summary>
    /// Revoga todos os tokens de um usuário por tipo
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="tokenType">Tipo do token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Número de tokens revogados</returns>
    public async Task<int> RevokeTokensByUserIdAsync(string userId, AccountTokenType tokenType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("ID do usuário não pode ser nulo ou vazio", nameof(userId));

            var tokensToRevoke = await _context.AccountTokens
                .Where(at => at.UserId == userId && 
                           at.TokenType == tokenType &&
                           at.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var revokedCount = 0;
            var revokedAt = DateTime.UtcNow;

            foreach (var token in tokensToRevoke)
            {
                token.RevokedAt = revokedAt;
                revokedCount++;
            }

            return revokedCount;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao revogar tokens do usuário {userId} do tipo {tokenType}", ex);
        }
    }

    /// <summary>
    /// Cria um novo token de conta
    /// </summary>
    /// <param name="token">Token a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token criado</returns>
    public async Task<AccountToken> CreateAsync(AccountToken token, CancellationToken cancellationToken = default)
    {
        return await AddAsync(token, cancellationToken);
    }
}