using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using BuildingBlocks.Data;

namespace AuthService.Domain.Repositories;

/// <summary>
/// Interface do repositório para a entidade AccountToken
/// Define operações específicas para gerenciamento de tokens de conta
/// </summary>
public interface IAccountTokenRepository : IRepository<AccountToken>
{
    /// <summary>
    /// Busca um token de conta pelo valor do token
    /// </summary>
    /// <param name="token">Valor do token a ser buscado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de conta encontrado ou null</returns>
    Task<AccountToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todos os tokens ativos de um usuário por tipo
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="tokenType">Tipo do token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de tokens ativos do usuário</returns>
    Task<IReadOnlyList<AccountToken>> GetActiveTokensByUserIdAsync(string userId, AccountTokenType tokenType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga todos os tokens de um usuário por tipo
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="tokenType">Tipo do token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Número de tokens revogados</returns>
    Task<int> RevokeTokensByUserIdAsync(string userId, AccountTokenType tokenType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um novo token de conta
    /// </summary>
    /// <param name="token">Token a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token criado</returns>
    Task<AccountToken> CreateAsync(AccountToken token, CancellationToken cancellationToken = default);
}