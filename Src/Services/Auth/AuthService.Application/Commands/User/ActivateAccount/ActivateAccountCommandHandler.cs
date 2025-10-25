using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.ActivateAccount;

/// <summary>
/// Handler responsável por processar o comando de ativação de conta de usuário
/// Implementa toda a lógica de confirmação de email usando o token fornecido
/// </summary>
public class ActivateAccountCommandHandler : IRequestHandler<ActivateAccountCommand, ApiResponse<ActivateAccountResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly ILogger<ActivateAccountCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="logger">Logger para registro de operações</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    public ActivateAccountCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        ILogger<ActivateAccountCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Processa o comando de ativação de conta de usuário de forma assíncrona
    /// </summary>
    /// <param name="request">Dados do token e userId para ativação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com resultado da operação</returns>
    public async Task<ApiResponse<ActivateAccountResponse>> HandleAsync(ActivateAccountCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔑 Iniciando processo de ativação de conta para o usuário: {UserId}", request.UserId);

        try
        {
            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Buscar o usuário pelo ID
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("⚠️ Usuário não encontrado: {UserId}", request.UserId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ActivateAccountResponse>.Fail(
                    new List<Error> { new("Usuário não encontrado") }
                );
            }

            // 2. Verificar se o email já está confirmado
            if (user.EmailConfirmed)
            {
                _logger.LogInformation("ℹ️ Email já confirmado para o usuário: {UserId} - {Email}", user.Id, user.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ActivateAccountResponse>.Fail(
                    new List<Error> { new("Esta conta já foi ativada anteriormente") }
                );
            }

            // 3. Confirmar o email usando o token
            _logger.LogInformation("✅ Confirmando email para o usuário: {UserId} - {Email}", user.Id, user.Email);
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new Error(e.Description)).ToList();
                _logger.LogError("❌ Falha ao confirmar email para usuário {UserId}: {Errors}", 
                    user.Id, string.Join(", ", errors.Select(e => e.Message)));
                
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ActivateAccountResponse>.Fail(errors);
            }

            // 4. Atualizar dados do usuário
            user.UpdatedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow; // Marcar como primeiro acesso
            
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("❌ Falha ao atualizar dados do usuário {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<ActivateAccountResponse>.Fail(
                    new List<Error> { new("Erro ao finalizar ativação da conta") }
                );
            }

            // 5. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 6. Retornar resposta de sucesso
            var response = new ActivateAccountResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                ActivatedAt = DateTime.UtcNow,
                IsActivated = true
            };

            _logger.LogInformation("🎉 Conta ativada com sucesso para usuário {UserId} - {Email}", 
                user.Id, user.Email);

            return ApiResponse<ActivateAccountResponse>.Ok(response, "Conta ativada com sucesso!");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de ativação cancelada para usuário {UserId}", request.UserId);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<ActivateAccountResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante ativação da conta do usuário {UserId}", request.UserId);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<ActivateAccountResponse>.Fail(
                new List<Error> { new("Erro interno durante a ativação. Tente novamente.") }
            );
        }
    }
}
