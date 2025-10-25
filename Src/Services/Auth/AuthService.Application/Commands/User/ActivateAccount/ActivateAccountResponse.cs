namespace AuthService.Application.Commands.User.ActivateAccount;

/// <summary>
/// Response para o comando de ativação de conta
/// </summary>
public class ActivateAccountResponse
{
    /// <summary>
    /// ID do usuário que foi ativado
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário que foi ativado
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora da ativação
    /// </summary>
    public DateTime ActivatedAt { get; set; }

    /// <summary>
    /// Indica se a conta foi ativada com sucesso
    /// </summary>
    public bool IsActivated { get; set; }
}
