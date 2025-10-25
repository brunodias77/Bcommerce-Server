using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.ForgotPassword;

/// <summary>
/// Handler para o comando de solicitação de redefinição de senha
/// Implementa a lógica de negócio para gerar token de redefinição e enviar email
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<ForgotPasswordResult>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    public ForgotPasswordCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa o comando de solicitação de redefinição de senha
    /// </summary>
    /// <param name="request">Comando com dados da solicitação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da operação</returns>
    public async Task<ApiResponse<ForgotPasswordResult>> HandleAsync(ForgotPasswordCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processamento de solicitação de redefinição de senha para email: {Email}", request.Email);

        try
        {
            // Validar entrada
            ValidateRequest(request);

            // Buscar usuário por email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado para email: {Email}", request.Email);
                
                // Por segurança, retornamos sucesso mesmo quando o usuário não existe
                // para não revelar informações sobre contas existentes
                return ApiResponse<ForgotPasswordResult>.Ok(new ForgotPasswordResult
                {
                    Success = true,
                    Message = "Se o email estiver cadastrado, você receberá instruções para redefinir sua senha.",
                    TokenGenerated = false
                });
            }

            // Verificar se o email foi confirmado
            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
            {
                _logger.LogWarning("Tentativa de redefinição de senha para email não confirmado: {Email}", request.Email);
                return ApiResponse<ForgotPasswordResult>.Ok(new ForgotPasswordResult
                {
                    Success = true,
                    Message = "Se o email estiver cadastrado, você receberá instruções para redefinir sua senha.",
                    TokenGenerated = false
                });
            }

            // Iniciar transação para garantir consistência
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Gerar token de redefinição usando Identity
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                _logger.LogInformation("Token de redefinição gerado com sucesso para usuário: {UserId}", user.Id);

                // Salvar mudanças antes de enviar email
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Enviar email de redefinição
                var emailSent = await _emailService.SendPasswordResetAsync(user.Email!, resetToken, Guid.Parse(user.Id), cancellationToken);

                if (!emailSent)
                {
                    _logger.LogError("Falha ao enviar email de redefinição para {Email}", user.Email);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ForgotPasswordResult>.Fail(
                        "EMAIL_SEND_ERROR",
                        "Erro ao enviar email de redefinição. Tente novamente mais tarde.");
                }

                // Confirmar transação após sucesso
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                _logger.LogInformation("Token de redefinição de senha gerado e email enviado com sucesso para usuário: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante geração de token e envio de email para usuário: {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            return ApiResponse<ForgotPasswordResult>.Ok(new ForgotPasswordResult
            {
                Success = true,
                Message = "Instruções para redefinir sua senha foram enviadas para seu email.",
                TokenGenerated = true
            });
        }
        catch (ValidationException validationEx)
        {
            _logger.LogWarning("Validação falhou para solicitação de redefinição de senha: {Errors}", 
                string.Join("; ", validationEx.Errors.Select(e => e.Message)));
            
            return ApiResponse<ForgotPasswordResult>.Fail(validationEx.Errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar solicitação de redefinição de senha para email: {Email}", request.Email);
            
            // Garantir rollback em caso de erro não tratado
            try
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Erro adicional durante rollback da transação");
            }
            
            return ApiResponse<ForgotPasswordResult>.Fail(
                "INTERNAL_SERVER_ERROR",
                "Erro interno do servidor. Tente novamente mais tarde.");
        }
    }

    /// <summary>
    /// Valida os dados da solicitação
    /// </summary>
    /// <param name="request">Dados da solicitação</param>
    private static void ValidateRequest(ForgotPasswordCommand request)
    {
        var validator = new ValidationHandler();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validator.Add("Email é obrigatório");
        }
        else if (!IsValidEmail(request.Email))
        {
            validator.Add("Email deve ter um formato válido");
        }

        validator.ThrowIfHasErrors();
    }

    /// <summary>
    /// Valida formato do email
    /// </summary>
    /// <param name="email">Email a ser validado</param>
    /// <returns>True se o email for válido</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}