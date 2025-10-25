using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.Logout;

/// <summary>
/// Handler responsável por processar o comando de logout de usuário
/// Implementa toda a lógica de revogação de refresh tokens
/// </summary>
/// <remarks>
/// Este handler utiliza o serviço LoggedUser para obter automaticamente o usuário autenticado
/// através do token JWT, não necessitando receber o UserId como parâmetro
/// </remarks>
public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, ApiResponse<LogoutUserResponse>>
{
    private readonly ILoggedUser _loggedUser;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutUserCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="loggedUser">Serviço para obter informações do usuário autenticado</param>
    /// <param name="refreshTokenRepository">Repositório para operações com refresh tokens</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="logger">Logger para registro de operações</param>
    /// <exception cref="ArgumentNullException">Lançado quando algum parâmetro é nulo</exception>
    public LogoutUserCommandHandler(
        ILoggedUser loggedUser,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<LogoutUserCommandHandler> logger)
    {
        _loggedUser = loggedUser ?? throw new ArgumentNullException(nameof(loggedUser));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa o comando de logout de forma assíncrona
    /// </summary>
    /// <param name="request">Comando de logout</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação de logout</returns>
    /// <exception cref="UnauthorizedAccessException">Lançado quando o usuário não está autenticado</exception>
    /// <exception cref="InvalidOperationException">Lançado quando o usuário não é encontrado</exception>
    public async Task<ApiResponse<LogoutUserResponse>> HandleAsync(LogoutUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 Iniciando processo de logout para usuário autenticado");

        try
        {
            // 1. Obter usuário autenticado via token JWT
            var user = await _loggedUser.User();
            _logger.LogInformation("✅ Usuário {UserId} - {Email} identificado via JWT", user.Id, user.Email);

            // 2. Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogInformation("📦 Transação iniciada para logout do usuário {UserId}", user.Id);

            int revokedTokensCount = 0;

            // 3. Revogar tokens baseado na estratégia
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                // Revogar apenas o token específico
                revokedTokensCount = await RevokeSpecificTokenAsync(user.Id, request.RefreshToken, cancellationToken);
                _logger.LogInformation("🔒 Token específico revogado para usuário {UserId}. Tokens revogados: {Count}", 
                    user.Id, revokedTokensCount);
            }
            else
            {
                // Revogar todos os tokens ativos do usuário
                revokedTokensCount = await RevokeAllUserTokensAsync(user.Id, cancellationToken);
                _logger.LogInformation("🔒 Todos os tokens revogados para usuário {UserId}. Tokens revogados: {Count}", 
                    user.Id, revokedTokensCount);
            }

            // 4. Salvar alterações
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                _logger.LogInformation("💾 Alterações salvas com sucesso para logout do usuário {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao salvar alterações para logout do usuário {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LogoutUserResponse>.Fail(
                    new List<Error> { new("Erro interno durante logout. Tente novamente.") }
                );
            }

            // 5. Retornar resposta de sucesso
            var response = new LogoutUserResponse
            {
                Success = true,
                Message = revokedTokensCount > 0 
                    ? $"Logout realizado com sucesso. {revokedTokensCount} token(s) revogado(s)."
                    : "Logout realizado com sucesso. Nenhum token ativo encontrado.",
                RevokedTokensCount = revokedTokensCount
            };

            _logger.LogInformation("🎉 Logout realizado com sucesso para usuário {UserId} - {Email}. Tokens revogados: {Count}", 
                user.Id, user.Email, revokedTokensCount);

            return ApiResponse<LogoutUserResponse>.Ok(response, "Logout realizado com sucesso");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de logout cancelada");
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<LogoutUserResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante logout");
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<LogoutUserResponse>.Fail(
                new List<Error> { new("Erro interno durante logout. Tente novamente.") }
            );
        }
    }

    /// <summary>
    /// Valida os dados de entrada do comando
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Resultado da validação</returns>
    private ValidationHandler ValidateRequest(LogoutUserCommand request)
    {
        var validation = new ValidationHandler();

        // Não há validações específicas necessárias, pois o usuário é obtido via token JWT
        // O RefreshToken é opcional

        return validation;
    }

    /// <summary>
    /// Revoga um refresh token específico
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="refreshToken">Token específico para revogar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Quantidade de tokens revogados</returns>
    private async Task<int> RevokeSpecificTokenAsync(string userId, string refreshToken, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Buscando token específico para usuário {UserId}", userId);

        var tokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && rt.Token == refreshToken && rt.RevokedAt == null,
            cancellationToken
        );

        var tokenToRevoke = tokens.FirstOrDefault();
        if (tokenToRevoke == null)
        {
            _logger.LogWarning("⚠️ Token específico não encontrado ou já revogado para usuário {UserId}", userId);
            return 0;
        }

        // Verificar se o token ainda está ativo
        if (!tokenToRevoke.IsActive)
        {
            _logger.LogWarning("⚠️ Token específico já expirado para usuário {UserId}", userId);
            return 0;
        }

        // Revogar o token
        tokenToRevoke.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepository.Update(tokenToRevoke);

        _logger.LogInformation("✅ Token específico revogado para usuário {UserId}", userId);
        return 1;
    }

    /// <summary>
    /// Revoga todos os refresh tokens ativos de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Quantidade de tokens revogados</returns>
    private async Task<int> RevokeAllUserTokensAsync(string userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Buscando todos os tokens ativos para usuário {UserId}", userId);

        var activeTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (!activeTokens.Any())
        {
            _logger.LogInformation("ℹ️ Nenhum token ativo encontrado para usuário {UserId}", userId);
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

        _logger.LogInformation("✅ {Count} tokens revogados para usuário {UserId}", revokedCount, userId);
        return revokedCount;
    }
}