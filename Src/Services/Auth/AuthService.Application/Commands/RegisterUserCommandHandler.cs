using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands;

/// <summary>
/// Resposta do comando de registro de usuário
/// </summary>
public class RegisterUserResponse
{
    /// <summary>
    /// ID único do usuário criado
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Email do usuário para referência
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem informativa sobre o registro
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Handler para processar o comando de registro de usuário
/// Utiliza ASP.NET Core Identity para gerenciamento de usuários
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<RegisterUserResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    // Roles padrão do sistema
    private static readonly string[] DefaultRoles = { "User", "Admin", "Manager" };
    private const string DefaultUserRole = "User";

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="roleManager">Gerenciador de roles do Identity</param>
    /// <param name="logger">Logger para registrar eventos</param>
    /// <param name="unitOfWork">Unit of Work para controle transacional</param>
    /// <param name="emailService">Serviço de envio de emails</param>
    public RegisterUserCommandHandler(
        UserManager<User> userManager,
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
    /// Processa o comando de registro de usuário
    /// </summary>
    /// <param name="request">Comando com dados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados do usuário criado e token de confirmação ou erro</returns>
    public async Task<ApiResponse<RegisterUserResponse>> HandleAsync(RegisterUserCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando registro de usuário para email: {Email}", request.Email);

            // Verificar se o email já existe
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de registro com email já existente: {Email}", request.Email);
                return ApiResponse<RegisterUserResponse>.Fail("EMAIL_ALREADY_EXISTS", "Este email já está em uso");
            }

            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Garantir que os roles padrão existam
                var rolesCreated = await EnsureDefaultRolesExistAsync(cancellationToken);
                if (!rolesCreated)
                {
                    _logger.LogError("Falha ao criar/verificar roles padrão do sistema");
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<RegisterUserResponse>.Fail("ROLES_CREATION_FAILED", "Erro ao configurar roles do sistema");
                }

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

                    // Adicionar role padrão ao usuário
                    var roleResult = await _userManager.AddToRoleAsync(user, DefaultUserRole);
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Falha ao adicionar role {Role} ao usuário {UserId}: {Errors}", 
                            DefaultUserRole, user.Id, roleErrors);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<RegisterUserResponse>.Fail("ROLE_ASSIGNMENT_FAILED", 
                            $"Falha ao atribuir role ao usuário: {roleErrors}");
                    }

                    _logger.LogInformation("Role {Role} atribuído com sucesso ao usuário {UserId}", 
                        DefaultUserRole, user.Id);

                    // Gerar token de confirmação de email
                    var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    if (string.IsNullOrEmpty(emailConfirmationToken))
                    {
                        _logger.LogError("Falha ao gerar token de confirmação de email para o usuário {UserId}", user.Id);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<RegisterUserResponse>.Fail("EMAIL_TOKEN_GENERATION_FAILED", "Falha ao gerar token de confirmação de email");
                    }

                    _logger.LogInformation("Token de confirmação de email gerado com sucesso para o usuário {UserId}", user.Id);

                    // Enviar email de confirmação
                    var emailSent = await _emailService.SendEmailConfirmationAsync(user.Email!, emailConfirmationToken, Guid.Parse(user.Id), cancellationToken);
                    if (!emailSent)
                    {
                        _logger.LogError("Falha ao enviar email de confirmação para o usuário {UserId} ({Email})", user.Id, user.Email);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<RegisterUserResponse>.Fail("EMAIL_SEND_FAILED", "Falha ao enviar email de confirmação");
                    }

                    _logger.LogInformation("Email de confirmação enviado com sucesso para o usuário {UserId} ({Email})", user.Id, user.Email);

                    // Salvar mudanças no contexto
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Commit da transação
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    // Converter string ID para Guid
                    if (Guid.TryParse(user.Id, out var userId))
                    {
                        var response = new RegisterUserResponse
                        {
                            UserId = userId,
                            Email = user.Email!,
                            Message = "Usuário registrado com sucesso. Verifique seu email para confirmar a conta."
                        };

                        _logger.LogInformation("Usuário {UserId} registrado com sucesso. Email de confirmação enviado para {Email}.", userId, user.Email);
                        return ApiResponse<RegisterUserResponse>.Ok(response, "Usuário registrado com sucesso. Verifique seu email para confirmar a conta.");
                    }
                    else
                    {
                        _logger.LogError("Erro ao converter ID do usuário para Guid: {UserId}", user.Id);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<RegisterUserResponse>.Fail("ID_CONVERSION_ERROR", "Erro interno ao processar ID do usuário");
                    }
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao criar usuário: {Errors}", errors);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<RegisterUserResponse>.Fail("REGISTRATION_FAILED", $"Falha ao registrar usuário: {errors}");
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
            return ApiResponse<RegisterUserResponse>.Fail("INTERNAL_ERROR", "Erro interno do servidor ao processar registro");
        }
    }

    /// <summary>
    /// Garante que os roles padrão do sistema existam
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se todos os roles foram criados/verificados com sucesso</returns>
    private async Task<bool> EnsureDefaultRolesExistAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var roleName in DefaultRoles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogInformation("Criando role padrão: {RoleName}", roleName);
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);
                    
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError("Falha ao criar role {RoleName}: {Errors}", roleName, errors);
                        return false;
                    }
                    
                    _logger.LogInformation("Role {RoleName} criado com sucesso", roleName);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao verificar/criar roles padrão");
            return false;
        }
    }
}