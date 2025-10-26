namespace AuthService.Application.Commands.User.Logout;

/// <summary>
/// Response do comando de logout de usuário
/// </summary>
public record LogoutUserResponse
{
    /// <summary>
    /// Indica se o logout foi realizado com sucesso
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Mensagem de feedback sobre a operação de logout
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Quantidade de refresh tokens que foram revogados durante o logout
    /// </summary>
    public int RevokedTokensCount { get; init; }
}
