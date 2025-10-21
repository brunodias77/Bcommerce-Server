namespace AuthService.Domain.Enums;

/// <summary>
/// Tipos de tokens de conta disponíveis no sistema
/// </summary>
public enum AccountTokenType
{
    /// <summary>
    /// Token para ativação de conta de usuário
    /// </summary>
    ACCOUNT_ACTIVATION,

    /// <summary>
    /// Token para redefinição de senha
    /// </summary>
    PASSWORD_RESET
}