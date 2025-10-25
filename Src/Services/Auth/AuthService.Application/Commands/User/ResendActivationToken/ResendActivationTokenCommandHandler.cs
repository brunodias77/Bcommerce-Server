using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.ResendActivationToken;

/// <summary>
/// Handler responsável por processar o comando de reenvio de token de ativação
/// Implementa toda a lógica de validação e reenvio de email de confirmação
/// </summary>
public class ResendActivationTokenCommandHandler : IRequestHandler<ResendActivationTokenCommand, ApiResponse<ResendActivationTokenResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly ILogger<ResendActivationTokenCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="logger">Logger para registro de operações</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="emailService">Serviço de envio de emails</param>
    public ResendActivationTokenCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        ILogger<ResendActivationTokenCommandHandler> logger,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    /// <summary>
    /// Processa o comando de reenvio de token de ativação de forma assíncrona
    /// </summary>
    /// <param name="request">Dados do usuário que deseja reenviar o token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com resultado da operação</returns>
    public async Task<ApiResponse<ResendActivationTokenResponse>> HandleAsync(ResendActivationTokenCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Iniciando processo de reenvio de token de ativação para o email: {Email}", request.Email);

        try
        {
            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Verificar se o usuário existe
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ Tentativa de reenvio de token para email não cadastrado: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ResendActivationTokenResponse>.Fail(
                    new List<Error> { new("Usuário não encontrado com este endereço de email") }
                );
            }

            // 2. Verificar se o usuário já está confirmado
            if (user.EmailConfirmed)
            {
                _logger.LogWarning("⚠️ Tentativa de reenvio de token para usuário já confirmado: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ResendActivationTokenResponse>.Fail(
                    new List<Error> { new("Esta conta já foi ativada. Você pode fazer login normalmente.") }
                );
            }

            _logger.LogInformation("✅ Usuário encontrado e não confirmado: {UserId} - {Email}", user.Id, user.Email);

            // 3. Gerar novo token de confirmação de email
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogInformation("🔑 Novo token de confirmação gerado para usuário {UserId}", user.Id);

            // 4. Enviar email de confirmação
            var emailSent = await _emailService.SendEmailConfirmationAsync(
                user.Email!, 
                confirmationToken, 
                Guid.Parse(user.Id), 
                cancellationToken
            );

            if (!emailSent)
            {
                _logger.LogError("📧❌ Falha ao enviar email de confirmação para {Email}", user.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ResendActivationTokenResponse>.Fail(
                    new List<Error> { new("Falha ao enviar email de confirmação. Tente novamente em alguns minutos.") }
                );
            }

            _logger.LogInformation("📧✅ Email de confirmação reenviado com sucesso para {Email}", user.Email);

            // 5. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 6. Retornar resposta de sucesso
            var response = new ResendActivationTokenResponse
            {
                Email = user.Email!,
                Message = "Token de ativação reenviado com sucesso! Verifique seu email para confirmar a conta.",
                SentAt = DateTime.UtcNow
            };

            _logger.LogInformation("🎉 Reenvio de token concluído com sucesso para usuário {UserId} - {Email}", 
                user.Id, user.Email);

            return ApiResponse<ResendActivationTokenResponse>.Ok(response, "Token de ativação reenviado com sucesso");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de reenvio de token cancelada para {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<ResendActivationTokenResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante reenvio de token para {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<ResendActivationTokenResponse>.Fail(
                new List<Error> { new("Erro interno durante o reenvio. Tente novamente.") }
            );
        }
    }
}