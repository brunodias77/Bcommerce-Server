namespace AuthService.Application.Commands.User.UpdateProfile;

/// <summary>
/// Resposta do comando de atualização de perfil
/// </summary>
public record UpdateProfileResponse
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