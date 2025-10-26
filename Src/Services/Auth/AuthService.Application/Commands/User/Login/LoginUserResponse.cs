namespace AuthService.Application.Commands.User.Login;

/// <summary>
/// Response do comando de login do usuário
/// </summary>
public record LoginUserResponse
{
    /// <summary>
    /// Token de acesso JWT
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Token de refresh para renovação do access token
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Tempo de expiração do token em segundos
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Tipo do token (Bearer)
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// ID do usuário
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Email do usuário
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string FullName { get; init; } = string.Empty;
};