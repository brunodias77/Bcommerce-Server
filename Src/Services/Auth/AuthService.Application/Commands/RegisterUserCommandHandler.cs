using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;

namespace AuthService.Application.Commands;

/// <summary>
/// Handler para processar o comando de registro de usuário
/// Utiliza ASP.NET Core Identity para gerenciamento de usuários
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<Guid>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="logger">Logger para registrar eventos</param>
    /// <param name="unitOfWork">Unit of Work para controle transacional</param>
    public RegisterUserCommandHandler(
        UserManager<User> userManager,
        ILogger<RegisterUserCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Processa o comando de registro de usuário
    /// </summary>
    /// <param name="request">Comando com dados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com ID do usuário criado ou erro</returns>
    public async Task<ApiResponse<Guid>> HandleAsync(RegisterUserCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando registro de usuário para email: {Email}", request.Email);

            // Verificar se o email já existe
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de registro com email já existente: {Email}", request.Email);
                return ApiResponse<Guid>.Fail("EMAIL_ALREADY_EXISTS", "Este email já está em uso");
            }

            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Criar novo usuário
                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    BirthDate = request.BirthDate,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Tentar criar o usuário
                var result = await _userManager.CreateAsync(user, request.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuário criado com sucesso. ID: {UserId}", user.Id);

                    // Salvar mudanças no contexto
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Commit da transação
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    // Converter string ID para Guid
                    if (Guid.TryParse(user.Id, out var userId))
                    {
                        return ApiResponse<Guid>.Ok(userId);
                    }
                    else
                    {
                        _logger.LogError("Erro ao converter ID do usuário para Guid: {UserId}", user.Id);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<Guid>.Fail("ID_CONVERSION_ERROR", "Erro interno ao processar ID do usuário");
                    }
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao criar usuário: {Errors}", errors);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<Guid>.Fail("REGISTRATION_FAILED", $"Falha ao registrar usuário: {errors}");
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Erro durante transação de registro de usuário");
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao registrar usuário com email: {Email}", request.Email);
            return ApiResponse<Guid>.Fail("INTERNAL_ERROR", "Erro interno do servidor ao processar registro");
        }
    }
}