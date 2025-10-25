namespace AuthService.Application.Commands.User.RefreshToken;

/// <summary>
/// Response do comando de refresh token
/// </summary>
public record RefreshTokenResponse
{
    /// <summary>
    /// Novo access token JWT gerado
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Novo refresh token gerado
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração do access token
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Tipo do token (sempre "Bearer")
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Tempo de vida do token em segundos
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// ID do usuário
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Email do usuário
    /// </summary>
    public string Email { get; init; } = string.Empty;
};
