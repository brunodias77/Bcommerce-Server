using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Services.Token;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.RefreshToken;

/// <summary>
/// Handler responsável por processar o comando de refresh token
/// Implementa toda a lógica de renovação de tokens JWT usando refresh token
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<RefreshTokenResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly ITokenJwtService _tokenJwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="tokenJwtService">Serviço de geração de tokens JWT</param>
    /// <param name="refreshTokenRepository">Repositório para operações com refresh tokens</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="logger">Logger para registro de operações</param>
    public RefreshTokenCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        ITokenJwtService tokenJwtService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _tokenJwtService = tokenJwtService ?? throw new ArgumentNullException(nameof(tokenJwtService));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa o comando de refresh token
    /// </summary>
    /// <param name="request">Comando contendo access token expirado e refresh token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Response com novos tokens gerados</returns>
    public async Task<ApiResponse<RefreshTokenResponse>> HandleAsync(RefreshTokenCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔄 Iniciando processo de refresh token");

            // 1. Validar entrada
            var validationResult = ValidateRequest(request);
            if (validationResult.HasErrors)
            {
                _logger.LogWarning("❌ Dados de entrada inválidos para refresh token");
                return ApiResponse<RefreshTokenResponse>.Fail(validationResult);
            }

            // 2. Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 3. Extrair userId do access token expirado
            var userId = await _tokenJwtService.GetUserIdFromTokenAsync(request.AccessToken);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("❌ Não foi possível extrair userId do access token");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RefreshTokenResponse>.Fail(
                    new List<Error> { new("Token de acesso inválido") }
                );
            }

            _logger.LogInformation("✅ UserId extraído do token: {UserId}", userId);

            // 4. Buscar refresh token no banco
            var refreshTokens = await _refreshTokenRepository.FindAsync(
                rt => rt.Token == request.RefreshToken && rt.UserId == userId,
                cancellationToken
            );

            var refreshTokenEntity = refreshTokens.FirstOrDefault();
            if (refreshTokenEntity == null)
            {
                _logger.LogWarning("❌ Refresh token não encontrado para usuário {UserId}", userId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RefreshTokenResponse>.Fail(
                    new List<Error> { new("Refresh token inválido") }
                );
            }

            // 5. Validar se o refresh token está ativo
            if (!refreshTokenEntity.IsActive)
            {
                _logger.LogWarning("❌ Refresh token inativo para usuário {UserId}. Expirado: {ExpiresAt}, Revogado: {RevokedAt}", 
                    userId, refreshTokenEntity.ExpiresAt, refreshTokenEntity.RevokedAt);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RefreshTokenResponse>.Fail(
                    new List<Error> { new("Refresh token expirado ou revogado") }
                );
            }

            _logger.LogInformation("✅ Refresh token válido encontrado para usuário {UserId}", userId);

            // 6. Buscar usuário
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("❌ Usuário {UserId} não encontrado", userId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RefreshTokenResponse>.Fail(
                    new List<Error> { new("Usuário não encontrado") }
                );
            }

            // 7. Buscar roles do usuário
            var userRoles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("✅ Usuário {UserId} encontrado com {RoleCount} roles", userId, userRoles.Count);

            // 8. Gerar novos tokens
            var newAccessToken = await _tokenJwtService.GenerateAccessTokenAsync(user, userRoles);
            var newRefreshToken = await _tokenJwtService.GenerateRefreshTokenAsync();

            _logger.LogInformation("✅ Novos tokens gerados para usuário {UserId}", userId);

            // 9. Revogar o refresh token antigo
            refreshTokenEntity.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepository.Update(refreshTokenEntity);

            _logger.LogInformation("✅ Refresh token antigo revogado para usuário {UserId}", userId);

            // 10. Criar novo refresh token
            var newRefreshTokenEntity = new Domain.Entities.RefreshToken
            {
                UserId = userId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 dias de validade
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("✅ Novo refresh token salvo para usuário {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao salvar novo refresh token para usuário {UserId}", userId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RefreshTokenResponse>.Fail(
                    new List<Error> { new("Erro interno durante renovação do token. Tente novamente.") }
                );
            }

            // 11. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 12. Calcular data de expiração do access token
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // Assumindo 60 minutos de validade
            var expiresIn = 3600; // 60 minutos em segundos

            // 13. Retornar resposta de sucesso
            var response = new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                UserId = userId,
                Email = user.Email ?? string.Empty
            };

            _logger.LogInformation("🎉 Refresh token realizado com sucesso para usuário {UserId} - {Email}", 
                userId, user.Email);

            return ApiResponse<RefreshTokenResponse>.Ok(response, "Tokens renovados com sucesso");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de refresh token cancelada");
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<RefreshTokenResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante refresh token");
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<RefreshTokenResponse>.Fail(
                new List<Error> { new("Erro interno durante renovação do token. Tente novamente.") }
            );
        }
    }

    /// <summary>
    /// Valida os dados de entrada do comando
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Handler de validação com os erros encontrados</returns>
    private static ValidationHandler ValidateRequest(RefreshTokenCommand request)
    {
        var handler = new ValidationHandler();

        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            handler.Add("Access token é obrigatório");
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            handler.Add("Refresh token é obrigatório");
        }

        return handler;
    }
}