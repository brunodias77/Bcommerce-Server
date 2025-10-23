namespace AuthService.Domain.Services;

public interface IEmailService
{
    /// <summary>
    /// Envia email de confirmação para o usuário
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="confirmationToken">Token de confirmação (será incluído no link)</param>
    /// <param name="userId">ID do usuário para referência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi enviado com sucesso</returns>
    Task<bool> SendEmailConfirmationAsync(string email, string confirmationToken, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de redefinição de senha
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="resetToken">Token de redefinição de senha</param>
    /// <param name="userId">ID do usuário para referência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi enviado com sucesso</returns>
    Task<bool> SendPasswordResetAsync(string email, string resetToken, Guid userId, CancellationToken cancellationToken = default);
}