using System.Security.Claims;
using AuthService.Domain.Entities;

namespace AuthService.Domain.Services.Token;

/// <summary>
/// Interface para serviços de geração e validação de tokens JWT
/// </summary>
public interface ITokenJwtService
{
    /// <summary>
    /// Gera um access token JWT para o usuário com suas roles
    /// </summary>
    /// <param name="user">Usuário para o qual gerar o token</param>
    /// <param name="roles">Roles do usuário</param>
    /// <returns>Token JWT como string</returns>
    Task<string> GenerateAccessTokenAsync(User user, IList<string> roles);

    /// <summary>
    /// Gera um refresh token aleatório
    /// </summary>
    /// <returns>Refresh token como string</returns>
    Task<string> GenerateRefreshTokenAsync();

    /// <summary>
    /// Valida um token JWT
    /// </summary>
    /// <param name="token">Token a ser validado</param>
    /// <returns>ClaimsPrincipal se válido, null se inválido</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);

    /// <summary>
    /// Obtém o ClaimsPrincipal de um token expirado (para refresh)
    /// </summary>
    /// <param name="token">Token expirado</param>
    /// <returns>ClaimsPrincipal se o token for válido (exceto expiração)</returns>
    Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token);

    /// <summary>
    /// Extrai o ID do usuário de um token JWT
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>ID do usuário ou null se não encontrado</returns>
    Task<string?> GetUserIdFromTokenAsync(string token);
}