namespace AuthService.Application.Commands.User.ChangePassword;

/// <summary>
/// Resposta do comando de alteração de senha
/// </summary>
public record ChangePasswordResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Mensagem descritiva do resultado da operação
    /// </summary>
    public string Message { get; init; } = string.Empty;
};