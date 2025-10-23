using AuthService.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Implementação fictícia do serviço de email para desenvolvimento e testes
/// ATENÇÃO: Este é um serviço simulado que não envia emails reais
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly Random _random;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _random = new Random();
    }

    /// <summary>
    /// Simula o envio de email de confirmação para o usuário
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="confirmationToken">Token de confirmação (será incluído no link)</param>
    /// <param name="userId">ID do usuário para referência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi "enviado" com sucesso</returns>
    public async Task<bool> SendEmailConfirmationAsync(string email, string confirmationToken, Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Validações de entrada
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogError("Tentativa de envio de email de confirmação com email vazio ou nulo");
            return false;
        }

        if (string.IsNullOrWhiteSpace(confirmationToken))
        {
            _logger.LogError("Tentativa de envio de email de confirmação com token vazio ou nulo para {Email}", email);
            return false;
        }

        if (userId == Guid.Empty)
        {
            _logger.LogError("Tentativa de envio de email de confirmação com userId inválido para {Email}", email);
            return false;
        }

        try
        {
            _logger.LogInformation("🚀 [FICTÍCIO] Iniciando envio de email de confirmação para {Email} (UserId: {UserId})", 
                email, userId);

            // Simular latência de rede realista (500ms a 2s)
            var delay = _random.Next(500, 2000);
            await Task.Delay(delay, cancellationToken);

            // Simular falha ocasional (5% de chance)
            if (_random.NextDouble() < 0.05)
            {
                _logger.LogWarning("⚠️ [FICTÍCIO] Falha simulada no envio de email de confirmação para {Email}", email);
                return false;
            }

            // Log detalhado do "envio"
            _logger.LogInformation("📧 [FICTÍCIO] Email de confirmação enviado com sucesso!");
            _logger.LogInformation("   📍 Destinatário: {Email}", email);
            _logger.LogInformation("   🔑 Token: {Token}", MaskToken(confirmationToken));
            _logger.LogInformation("   👤 UserId: {UserId}", userId);
            _logger.LogInformation("   🔗 Link de confirmação: https://localhost:5001/auth/confirm-email?token={Token}&userId={UserId}", 
                confirmationToken, userId);
            _logger.LogInformation("   ⏱️ Tempo de envio simulado: {Delay}ms", delay);

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("❌ [FICTÍCIO] Envio de email de confirmação cancelado para {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 [FICTÍCIO] Erro inesperado ao enviar email de confirmação para {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Simula o envio de email de redefinição de senha
    /// </summary>
    /// <param name="email">Email do destinatário</param>
    /// <param name="resetToken">Token de redefinição de senha</param>
    /// <param name="userId">ID do usuário para referência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi "enviado" com sucesso</returns>
    public async Task<bool> SendPasswordResetAsync(string email, string resetToken, Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Validações de entrada
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogError("Tentativa de envio de email de reset com email vazio ou nulo");
            return false;
        }

        if (string.IsNullOrWhiteSpace(resetToken))
        {
            _logger.LogError("Tentativa de envio de email de reset com token vazio ou nulo para {Email}", email);
            return false;
        }

        if (userId == Guid.Empty)
        {
            _logger.LogError("Tentativa de envio de email de reset com userId inválido para {Email}", email);
            return false;
        }

        try
        {
            _logger.LogInformation("🚀 [FICTÍCIO] Iniciando envio de email de redefinição de senha para {Email} (UserId: {UserId})", 
                email, userId);

            // Simular latência de rede realista (300ms a 1.5s)
            var delay = _random.Next(300, 1500);
            await Task.Delay(delay, cancellationToken);

            // Simular falha ocasional (3% de chance - menor que confirmação)
            if (_random.NextDouble() < 0.03)
            {
                _logger.LogWarning("⚠️ [FICTÍCIO] Falha simulada no envio de email de reset para {Email}", email);
                return false;
            }

            // Log detalhado do "envio"
            _logger.LogInformation("📧 [FICTÍCIO] Email de redefinição de senha enviado com sucesso!");
            _logger.LogInformation("   📍 Destinatário: {Email}", email);
            _logger.LogInformation("   🔑 Token: {Token}", MaskToken(resetToken));
            _logger.LogInformation("   👤 UserId: {UserId}", userId);
            _logger.LogInformation("   🔗 Link de reset: https://localhost:5001/auth/reset-password?token={Token}&userId={UserId}", 
                resetToken, userId);
            _logger.LogInformation("   ⏱️ Tempo de envio simulado: {Delay}ms", delay);
            _logger.LogInformation("   ⚠️ IMPORTANTE: Este link expira em 15 minutos por segurança");

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("❌ [FICTÍCIO] Envio de email de reset cancelado para {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 [FICTÍCIO] Erro inesperado ao enviar email de reset para {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Mascara o token para logs seguros (mostra apenas os primeiros e últimos caracteres)
    /// </summary>
    /// <param name="token">Token a ser mascarado</param>
    /// <returns>Token mascarado para log seguro</returns>
    private static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "[VAZIO]";

        if (token.Length <= 8)
            return "***";

        return $"{token[..4]}...{token[^4..]}";
    }
}