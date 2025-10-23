using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.Register;

/// <summary>
/// Handler responsável por processar o comando de registro de usuário
/// Implementa toda a lógica de criação de conta, validações e envio de confirmação por email
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<RegisterUserResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="roleManager">Gerenciador de roles do Identity</param>
    /// <param name="logger">Logger para registro de operações</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="emailService">Serviço de envio de emails</param>
    public RegisterUserCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RegisterUserCommandHandler> logger,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    /// <summary>
    /// Processa o comando de registro de usuário de forma assíncrona
    /// </summary>
    /// <param name="request">Dados do usuário a ser registrado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com resultado da operação</returns>
    public async Task<ApiResponse<RegisterUserResponse>> HandleAsync(RegisterUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 Iniciando processo de registro para o email: {Email}", request.Email);

        try
        {
            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Verificar se o email já existe
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("⚠️ Tentativa de registro com email já existente: {Email}", request.Email);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RegisterUserResponse>.Fail(
                    new List<Error> { new("Este endereço de email já possui uma conta associada") }
                );
            }

            // 2. Criar o novo usuário
            var user = new Domain.Entities.User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.Phone,
                BirthDate = request.BirthDate,
                EmailConfirmed = false, // Inicialmente não confirmado
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("👤 Criando usuário: {Email} - {FullName}", request.Email, request.FullName);
            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => new Error(e.Description)).ToList();
                _logger.LogError("❌ Falha ao criar usuário {Email}: {Errors}", 
                    request.Email, string.Join(", ", errors.Select(e => e.Message)));
                
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RegisterUserResponse>.Fail(errors);
            }

            _logger.LogInformation("✅ Usuário criado com sucesso: {UserId}", user.Id);

            // 3. Garantir que as roles padrão existam
            await EnsureDefaultRolesExistAsync();

            // 4. Atribuir role padrão "User"
            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                _logger.LogError("❌ Falha ao atribuir role 'User' ao usuário {UserId}", user.Id);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<RegisterUserResponse>.Fail(
                    new List<Error> { new("Erro ao configurar permissões do usuário") }
                );
            }

            _logger.LogInformation("🔐 Role 'User' atribuída com sucesso ao usuário {UserId}", user.Id);

            // 5. Gerar token de confirmação de email
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            _logger.LogInformation("🔑 Token de confirmação gerado para usuário {UserId}", user.Id);

            // 6. Enviar email de confirmação
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
                
                return ApiResponse<RegisterUserResponse>.Fail(
                    new List<Error> { new("Usuário criado, mas falha ao enviar email de confirmação. Tente novamente.") }
                );
            }

            _logger.LogInformation("📧✅ Email de confirmação enviado com sucesso para {Email}", user.Email);

            // 7. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 8. Retornar resposta de sucesso
            var response = new RegisterUserResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                Message = "Usuário registrado com sucesso! Verifique seu email para confirmar a conta."
            };

            _logger.LogInformation("🎉 Registro concluído com sucesso para usuário {UserId} - {Email}", 
                user.Id, user.Email);

            return ApiResponse<RegisterUserResponse>.Ok(response, "Usuário registrado com sucesso");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⏹️ Operação de registro cancelada para {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<RegisterUserResponse>.Fail(
                new List<Error> { new("Operação cancelada") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado durante registro do usuário {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            
            return ApiResponse<RegisterUserResponse>.Fail(
                new List<Error> { new("Erro interno durante o registro. Tente novamente.") }
            );
        }
    }

    /// <summary>
    /// Garante que as roles padrão do sistema existam
    /// </summary>
    private async Task EnsureDefaultRolesExistAsync()
    {
        var defaultRoles = new[] { "User", "Admin", "Manager" };

        foreach (var roleName in defaultRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogInformation("🔧 Criando role padrão: {RoleName}", roleName);
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ Role '{RoleName}' criada com sucesso", roleName);
                }
                else
                {
                    _logger.LogError("❌ Falha ao criar role '{RoleName}': {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}