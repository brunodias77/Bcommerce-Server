using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Validations;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands.User.UpdateProfile;

/// <summary>
/// Handler responsável por processar o comando de atualização de perfil
/// Implementa toda a lógica de atualização de dados do usuário
/// Utiliza o serviço LoggedUser para identificar o usuário autenticado
/// </summary>
public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ApiResponse<UpdateProfileResponse>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;
    private readonly ILoggedUser _loggedUser;

    /// <summary>
    /// Construtor que recebe todas as dependências necessárias via injeção
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    /// <param name="unitOfWork">Unidade de trabalho para transações</param>
    /// <param name="logger">Logger para registro de operações</param>
    /// <param name="loggedUser">Serviço para obter o usuário autenticado</param>
    public UpdateProfileCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProfileCommandHandler> logger,
        ILoggedUser loggedUser)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggedUser = loggedUser ?? throw new ArgumentNullException(nameof(loggedUser));
    }

    /// <summary>
    /// Processa o comando de atualização de perfil
    /// </summary>
    /// <param name="request">Dados da requisição</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da operação</returns>
    public async Task<ApiResponse<UpdateProfileResponse>> HandleAsync(UpdateProfileCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔄 Iniciando atualização de perfil do usuário autenticado");

            // 1. Validar dados da requisição
            ValidateRequest(request);

            // 2. Obter usuário autenticado
            _logger.LogInformation("🔍 Obtendo usuário autenticado do token JWT");
            var user = await _loggedUser.User();
            
            _logger.LogInformation("👤 Usuário autenticado identificado: {UserId}", user.Id);

            // 3. Iniciar transação
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 4. Atualizar dados do usuário
            _logger.LogInformation("📝 Atualizando dados do usuário: {UserId}", user.Id);
            
            // Atualizar apenas os campos fornecidos
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                user.Phone = request.Phone.Trim();
            }
            else if (request.Phone == string.Empty)
            {
                // Se enviado string vazia, limpar o telefone
                user.Phone = null;
            }

            if (request.BirthDate.HasValue)
            {
                user.BirthDate = request.BirthDate.Value;
            }

            // Atualizar timestamp de modificação
            user.UpdatedAt = DateTime.UtcNow;

            // 5. Validar dados atualizados usando UserValidation
            _logger.LogInformation("🔍 Validando dados atualizados do usuário: {UserId}", user.Id);
            var validationResult = UserValidation.ValidateForUpdate(user);
            
            if (validationResult.HasErrors)
            {
                var validationErrors = validationResult.Errors.ToList();
                _logger.LogWarning("⚠️ Dados inválidos para o usuário {UserId}: {Errors}", 
                    user.Id, string.Join(", ", validationErrors.Select(e => e.Message)));
                
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                return ApiResponse<UpdateProfileResponse>.Fail(validationErrors);
            }

            // 6. Salvar alterações usando UserManager
            _logger.LogInformation("💾 Salvando alterações do usuário: {UserId}", user.Id);
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();
                _logger.LogError("❌ Erro ao atualizar usuário {UserId}: {Errors}", 
                    user.Id, string.Join(", ", errors));
                
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                var validation = new ValidationHandler();
                foreach (var error in errors)
                {
                    validation.Add(error);
                }
                return ApiResponse<UpdateProfileResponse>.Fail(validation);
            }

            // 7. Confirmar transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("✅ Perfil atualizado com sucesso para o usuário: {UserId}", user.Id);

            return ApiResponse<UpdateProfileResponse>.Ok(new UpdateProfileResponse
            {
                Success = true,
                Message = "Perfil atualizado com sucesso"
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("⚠️ Erro de validação ao atualizar perfil: {Errors}", string.Join(", ", ex.Errors.Select(e => e.Message)));
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            var validation = new ValidationHandler();
            foreach (var error in ex.Errors)
            {
                validation.Add(error.Message);
            }
            return ApiResponse<UpdateProfileResponse>.Fail(validation);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("🔒 Acesso não autorizado ao tentar atualizar perfil: {Message}", ex.Message);
            return ApiResponse<UpdateProfileResponse>.Fail("UNAUTHORIZED", "Usuário não autenticado ou token inválido");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("❌ Usuário não encontrado: {Message}", ex.Message);
            return ApiResponse<UpdateProfileResponse>.Fail("USER_NOT_FOUND", "Usuário não encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado ao atualizar perfil do usuário");
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            return ApiResponse<UpdateProfileResponse>.Fail("INTERNAL_ERROR", "Erro interno do servidor");
        }
    }

    /// <summary>
    /// Valida os dados da requisição
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    private static void ValidateRequest(UpdateProfileCommand request)
    {
        var validationHandler = new ValidationHandler();

        // Validar FullName se fornecido
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            if (request.FullName.Trim().Length < 2)
            {
                validationHandler.Add("O nome completo deve ter pelo menos 2 caracteres");
            }

            if (request.FullName.Length > 255)
            {
                validationHandler.Add("O nome completo não pode exceder 255 caracteres");
            }
        }

        // Validar Phone se fornecido
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            if (request.Phone.Length > 20)
            {
                validationHandler.Add("O telefone não pode exceder 20 caracteres");
            }
        }

        // Validar BirthDate se fornecido
        if (request.BirthDate.HasValue)
        {
            var today = DateTime.Today;
            var age = today.Year - request.BirthDate.Value.Year;

            if (request.BirthDate.Value.Date > today.AddYears(-age))
                age--;

            if (request.BirthDate.Value.Date > today)
            {
                validationHandler.Add("A data de nascimento não pode ser uma data futura");
            }

            if (age < 13)
            {
                validationHandler.Add("O usuário deve ter pelo menos 13 anos de idade");
            }

            if (age > 120)
            {
                validationHandler.Add("A data de nascimento não pode indicar uma idade superior a 120 anos");
            }
        }

        validationHandler.ThrowIfHasErrors();
    }
}