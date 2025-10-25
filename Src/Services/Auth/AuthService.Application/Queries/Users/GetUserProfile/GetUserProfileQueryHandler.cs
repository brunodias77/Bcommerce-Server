using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Validations;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Queries.Users.GetUserProfile;

/// <summary>
/// Handler responsável por processar a query GetUserProfile
/// </summary>
/// <remarks>
/// Este handler obtém o perfil completo do usuário autenticado através do token JWT,
/// retornando informações pessoais e de auditoria da conta
/// </remarks>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, ApiResponse<GetUserProfileResponse>>
{
    private readonly ILoggedUser _loggedUser;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    /// <summary>
    /// Inicializa uma nova instância do GetUserProfileQueryHandler
    /// </summary>
    /// <param name="loggedUser">Serviço para obter informações do usuário autenticado</param>
    /// <param name="logger">Logger para registrar operações e erros</param>
    /// <exception cref="ArgumentNullException">Lançado quando algum parâmetro é nulo</exception>
    public GetUserProfileQueryHandler(
        ILoggedUser loggedUser,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _loggedUser = loggedUser ?? throw new ArgumentNullException(nameof(loggedUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa a query GetUserProfile de forma assíncrona
    /// </summary>
    /// <param name="request">Query para obter perfil do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da API com dados do perfil do usuário</returns>
    /// <exception cref="UnauthorizedAccessException">Lançado quando o usuário não está autenticado</exception>
    /// <exception cref="InvalidOperationException">Lançado quando o usuário não é encontrado</exception>
    public async Task<ApiResponse<GetUserProfileResponse>> HandleAsync(GetUserProfileQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("👤 Iniciando busca do perfil do usuário autenticado");

        try
        {
            // Obter usuário autenticado via token JWT
            var user = await _loggedUser.User();

            _logger.LogInformation("✅ Perfil do usuário {UserId} obtido com sucesso", user.Id);

            // Mapear dados do usuário para a resposta
            var response = new GetUserProfileResponse(
                UserId: user.Id,
                Email: user.Email!,
                FullName: user.FullName,
                Phone: user.Phone ?? string.Empty,
                BirthDate: user.BirthDate,
                CreatedAt: user.CreatedAt,
                UpdatedAt: user.UpdatedAt,
                LastLoginAt: user.LastLoginAt,
                EmailConfirmed: user.EmailConfirmed
            );

            return ApiResponse<GetUserProfileResponse>.Ok(response, "Perfil do usuário obtido com sucesso");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("🚫 Tentativa de acesso não autorizado ao perfil: {Message}", ex.Message);
            return ApiResponse<GetUserProfileResponse>.Fail(
                new List<Error> { new("Usuário não autenticado") }
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("❌ Usuário não encontrado: {Message}", ex.Message);
            return ApiResponse<GetUserProfileResponse>.Fail(
                new List<Error> { new("Usuário não encontrado") }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Erro inesperado ao obter perfil do usuário");
            return ApiResponse<GetUserProfileResponse>.Fail(
                new List<Error> { new("Erro interno do servidor") }
            );
        }
    }
}