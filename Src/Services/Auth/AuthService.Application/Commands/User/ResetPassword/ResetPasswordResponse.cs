namespace AuthService.Application.Commands.User.ResetPassword;

/// <summary>
/// Resposta do comando de redefinição de senha
/// </summary>
public record ResetPasswordResponse
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