namespace AuthService.Application.Commands.User.ResendActivationToken;

/// <summary>
/// Resposta do comando de reenvio de token de ativação
/// </summary>
public record ResendActivationTokenResponse
{
    /// <summary>
    /// Email para o qual o token foi reenviado
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Mensagem de confirmação da operação
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora em que o email foi enviado
    /// </summary>
    public DateTime SentAt { get; init; }
};
