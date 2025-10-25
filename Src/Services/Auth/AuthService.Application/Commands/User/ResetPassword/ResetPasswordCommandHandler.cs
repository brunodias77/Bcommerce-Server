using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Repositories;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.ResetPassword;

/// <summary>
/// Handler para o comando de redefinição de senha
/// Implementa a lógica de negócio para redefinir senha usando token válido
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<ResetPasswordResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    public ResetPasswordCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa o comando de redefinição de senha
    /// </summary>
    public async Task<ApiResponse<ResetPasswordResponse>> HandleAsync(ResetPasswordCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando processo de redefinição de senha");

            // Validar entrada
            ValidateRequest(request);

            // Iniciar transação para garantir consistência
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Buscar usuário pelo email fornecido
                var user = await _userManager.FindByEmailAsync(request.Email);
                
                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado para o email: {Email}", request.Email);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ResetPasswordResponse>.Fail(
                        "USER_NOT_FOUND",
                        "Usuário não encontrado.");
                }

                // Verificar se o token é válido para este usuário específico
                var isValidToken = await _userManager.VerifyUserTokenAsync(
                    user, 
                    _userManager.Options.Tokens.PasswordResetTokenProvider, 
                    "ResetPassword", 
                    request.Token);

                if (!isValidToken)
                {
                    _logger.LogWarning("Token de redefinição inválido ou expirado para usuário: {UserId}", user.Id);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ResetPasswordResponse>.Fail(
                        "INVALID_TOKEN",
                        "Token inválido ou expirado.");
                }

                _logger.LogInformation("Token válido encontrado para usuário: {UserId}", user.Id);

                // Verificar se o usuário está ativo (email confirmado)
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Tentativa de reset de senha para usuário com email não confirmado: {UserId}", user.Id);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<ResetPasswordResponse>.Fail(
                        "USER_INACTIVE",
                        "Usuário não está ativo");
                }

                // Redefinir a senha usando o Identity
                var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                
                if (!resetResult.Succeeded)
                {
                    var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    _logger.LogError("Falha ao redefinir senha para usuário {UserId}: {Errors}", user.Id, errors);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    return ApiResponse<ResetPasswordResponse>.Fail(
                        resetResult.Errors.Select(e => new Error(e.Description)).ToList());
                }

                _logger.LogInformation("Senha redefinida com sucesso para usuário: {UserId}", user.Id);

                // Revogar todos os refresh tokens do usuário por segurança
                var userRefreshTokens = await _refreshTokenRepository.FindAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null, cancellationToken);
                foreach (var refreshToken in userRefreshTokens)
                {
                    refreshToken.RevokedAt = DateTime.UtcNow;
                    _refreshTokenRepository.Update(refreshToken);
                }
                _logger.LogInformation("Todos os refresh tokens do usuário foram revogados: {UserId} - Total: {Count}", user.Id, userRefreshTokens.Count);

                // Atualizar dados do usuário
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("Falha ao atualizar dados do usuário após redefinição de senha: {UserId}", user.Id);
                }

                // Salvar mudanças
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Confirmar transação
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Redefinição de senha concluída com sucesso para usuário: {UserId}", user.Id);

                return ApiResponse<ResetPasswordResponse>.Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Senha redefinida com sucesso."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante redefinição de senha");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (ValidationException validationEx)
        {
            _logger.LogWarning("Validação falhou para redefinição de senha: {Errors}", 
                string.Join("; ", validationEx.Errors.Select(e => e.Message)));
            
            return ApiResponse<ResetPasswordResponse>.Fail(validationEx.Errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar redefinição de senha");
            
            // Garantir rollback em caso de erro não tratado
            try
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Erro adicional durante rollback da transação");
            }
            
            return ApiResponse<ResetPasswordResponse>.Fail(
                "INTERNAL_SERVER_ERROR",
                "Erro interno do servidor. Tente novamente mais tarde.");
        }
    }

    /// <summary>
    /// Valida os dados da requisição
    /// </summary>
    private void ValidateRequest(ResetPasswordCommand request)
    {
        var validationHandler = new ValidationHandler();

        // Validar email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validationHandler.Add("Email é obrigatório");
        }
        else if (!request.Email.Contains("@") || !request.Email.Contains("."))
        {
            validationHandler.Add("Email deve ter um formato válido");
        }

        // Validar token
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            validationHandler.Add("Token é obrigatório");
        }

        // Validar nova senha
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            validationHandler.Add("Nova senha é obrigatória");
        }
        else if (request.NewPassword.Length < 8)
        {
            validationHandler.Add("A nova senha deve ter pelo menos 8 caracteres");
        }

        // Validar confirmação de senha
        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            validationHandler.Add("Confirmação da senha é obrigatória");
        }

        // Validar se as senhas coincidem
        if (!string.IsNullOrWhiteSpace(request.NewPassword) && 
            !string.IsNullOrWhiteSpace(request.ConfirmPassword) && 
            request.NewPassword != request.ConfirmPassword)
        {
            validationHandler.Add("A nova senha e a confirmação devem ser iguais");
        }

        validationHandler.ThrowIfHasErrors();
    }
}