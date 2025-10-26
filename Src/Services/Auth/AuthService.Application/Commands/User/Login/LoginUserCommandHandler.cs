using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Services.Token;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.Login;

/// <summary>
/// Handler responsável por processar o comando de login de usuário
/// Implementa toda a lógica de autenticação, validação e geração de tokens JWT
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ApiResponse<LoginUserResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly SignInManager<Domain.Entities.User> _signInManager;
    private readonly ITokenJwtService _tokenJwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="signInManager">Gerenciador de login do Identity</param>
    /// <param name="tokenJwtService">Serviço de geração de tokens JWT</param>
    /// <param name="refreshTokenRepository">Repositório para operações com refresh tokens</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="logger">Logger para registro de operações</param>
    public LoginUserCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        SignInManager<Domain.Entities.User> signInManager,
        ITokenJwtService tokenJwtService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoginUserCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _tokenJwtService = tokenJwtService ?? throw new ArgumentNullException(nameof(tokenJwtService));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa o comando de login de usuário de forma assíncrona
    /// </summary>
    /// <param name="request">Dados de login do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com resultado da operação</returns>
    public async Task<ApiResponse<LoginUserResponse>> HandleAsync(LoginUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 Iniciando processo de login para o email: {Email}", request.Email);

        try
        {
            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Buscar usuário pelo email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ Tentativa de login com email não encontrado: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Email ou senha inválidos") }
                );
            }

            // 2. Verificar se a conta está confirmada
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("⚠️ Tentativa de login com conta não confirmada: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Conta não confirmada. Verifique seu email para confirmar a conta.") }
                );
            }

            // 3. Verificar se a conta está bloqueada
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("🔒 Tentativa de login com conta bloqueada: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Conta temporariamente bloqueada devido a múltiplas tentativas de login inválidas") }
                );
            }

            // 4. Verificar credenciais
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            
            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    _logger.LogWarning("🔒 Conta bloqueada após tentativa de login inválida: {Email}", request.Email);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    return ApiResponse<LoginUserResponse>.Fail(
                        new List<Error> { new("Conta bloqueada devido a múltiplas tentativas de login inválidas") }
                    );
                }

                _logger.LogWarning("❌ Credenciais inválidas para: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Email ou senha inválidos") }
                );
            }

            _logger.LogInformation("✅ Credenciais válidas para usuário: {UserId}", user.Id);

            // 5. Gerar tokens JWT
            _logger.LogInformation("🔑 Gerando tokens JWT para usuário: {UserId}", user.Id);
            
            // Obter roles do usuário
            var userRoles = await _userManager.GetRolesAsync(user);
            
            var accessToken = await _tokenJwtService.GenerateAccessTokenAsync(user, userRoles);
            var refreshToken = await _tokenJwtService.GenerateRefreshTokenAsync();

            // 6. Atualizar dados do usuário
            try
            {
                // Atualizar dados do usuário
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError("❌ Falha ao atualizar dados do usuário {UserId}: {Errors}", user.Id, errors);
                    
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    return ApiResponse<LoginUserResponse>.Fail(
                        new List<Error> { new("Erro interno durante o login. Tente novamente.") }
                    );
                }

                _logger.LogInformation("✅ Dados do usuário {UserId} atualizados com sucesso", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao atualizar dados do usuário {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Erro interno durante o login. Tente novamente.") }
                );
            }

            // 7. Agora salvar o refresh token usando o repository
            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 dias de validade
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Adicionar o refresh token usando o repository
                await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("✅ Refresh token salvo com sucesso via repository para usuário {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao salvar refresh token via repository para usuário {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<LoginUserResponse>.Fail(
                    new List<Error> { new("Erro interno durante o login. Tente novamente.") }
                );
            }

            // 8. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 9. Retornar resposta de sucesso
            var response = new LoginUserResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600, // 1 hora em segundos
                TokenType = "Bearer",
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName
            };

            _logger.LogInformation("🎉 Login realizado com sucesso para usuário {UserId} - {Email}", 
                user.Id, user.Email);

            return ApiResponse<LoginUserResponse>.Ok(response, "Login realizado com sucesso");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de login cancelada para {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<LoginUserResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante login do usuário {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<LoginUserResponse>.Fail(
                new List<Error> { new("Erro interno durante o login. Tente novamente.") }
            );
        }
    }
}