using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.ChangePassword;

/// <summary>
/// Handler para o comando de alteração de senha
/// Implementa a lógica de negócio para alterar senha do usuário autenticado
/// O usuário é obtido automaticamente via JWT token através do LoggedUser service
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<ChangePasswordResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggedUser _loggedUser;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    public ChangePasswordCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILoggedUser loggedUser,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _loggedUser = loggedUser ?? throw new ArgumentNullException(nameof(loggedUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa o comando de alteração de senha
    /// </summary>
    public async Task<ApiResponse<ChangePasswordResponse>> HandleAsync(ChangePasswordCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando alteração de senha para usuário autenticado");

            // Validar requisição
            ValidateRequest(request);

            // Obter usuário autenticado via JWT token
            var user = await _loggedUser.User();
            _logger.LogInformation("Alteração de senha para usuário: {UserId}", user.Id);

            // Verificar se a senha atual está correta
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Senha atual incorreta para usuário: {UserId}", user.Id);
                return ApiResponse<ChangePasswordResponse>.Fail(
                    "INVALID_CURRENT_PASSWORD",
                    "Senha atual incorreta.");
            }

            // Verificar se a nova senha é diferente da atual
            var isSamePassword = await _userManager.CheckPasswordAsync(user, request.NewPassword);
            if (isSamePassword)
            {
                _logger.LogWarning("Nova senha igual à atual para usuário: {UserId}", user.Id);
                return ApiResponse<ChangePasswordResponse>.Fail(
                    "SAME_PASSWORD",
                    "A nova senha deve ser diferente da senha atual.");
            }

            // Iniciar transação para garantir consistência
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Alterar senha usando Identity
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                
                if (!changePasswordResult.Succeeded)
                {
                    var errors = string.Join("; ", changePasswordResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao alterar senha para usuário {UserId}: {Errors}", user.Id, errors);
                    
                    return ApiResponse<ChangePasswordResponse>.Fail(
                        "PASSWORD_CHANGE_FAILED",
                        $"Falha ao alterar senha: {errors}");
                }

                // Atualizar UpdatedAt do usuário
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Revogar todos os refresh tokens para forçar re-login por segurança
                await RevokeAllUserTokensAsync(user.Id, cancellationToken);

                // Salvar mudanças
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Senha alterada com sucesso para usuário: {UserId}", user.Id);

                return ApiResponse<ChangePasswordResponse>.Ok(new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Senha alterada com sucesso."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante alteração de senha");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Usuário não autenticado tentando alterar senha");
            return ApiResponse<ChangePasswordResponse>.Fail(
                "UNAUTHORIZED",
                "Usuário não autenticado.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro ao obter usuário autenticado para alteração de senha");
            return ApiResponse<ChangePasswordResponse>.Fail(
                "INVALID_USER_STATE",
                "Estado do usuário inválido.");
        }
        catch (ValidationException validationEx)
        {
            _logger.LogWarning("Validação falhou para alteração de senha: {Errors}", 
                string.Join("; ", validationEx.Errors.Select(e => e.Message)));
            
            return ApiResponse<ChangePasswordResponse>.Fail(validationEx.Errors.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar alteração de senha");
            
            // Garantir rollback em caso de erro não tratado
            try
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Erro adicional durante rollback da transação");
            }
            
            return ApiResponse<ChangePasswordResponse>.Fail(
                "INTERNAL_SERVER_ERROR",
                "Erro interno do servidor. Tente novamente mais tarde.");
        }
    }

    /// <summary>
    /// Valida os dados da requisição
    /// Usuário não precisa mais ser validado pois é obtido via JWT token
    /// </summary>
    private void ValidateRequest(ChangePasswordCommand request)
    {
        var validationHandler = new ValidationHandler();

        // Validar senha atual
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            validationHandler.Add("Senha atual é obrigatória");
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

    /// <summary>
    /// Revoga todos os refresh tokens ativos de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Quantidade de tokens revogados</returns>
    private async Task<int> RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando todos os tokens ativos para usuário: {UserId}", userId);

        var activeTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (!activeTokens.Any())
        {
            _logger.LogInformation("Nenhum token ativo encontrado para usuário: {UserId}", userId);
            return 0;
        }

        var revokedCount = 0;
        var revokedAt = DateTime.UtcNow;

        foreach (var token in activeTokens)
        {
            token.RevokedAt = revokedAt;
            _refreshTokenRepository.Update(token);
            revokedCount++;
        }

        _logger.LogInformation("{Count} tokens revogados para usuário: {UserId}", revokedCount, userId);
        return revokedCount;
    }
}