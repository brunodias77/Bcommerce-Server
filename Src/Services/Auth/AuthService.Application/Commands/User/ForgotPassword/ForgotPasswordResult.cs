namespace AuthService.Application.Commands.User.ForgotPassword;

/// <summary>
/// Resultado do comando de solicitação de redefinição de senha
/// </summary>
public record ForgotPasswordResult
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Mensagem descritiva do resultado da operação
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Indica se um token de redefinição foi gerado
    /// </summary>
    public bool TokenGenerated { get; init; }
}
