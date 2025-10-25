using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Queries.Users.GetUserProfile;

/// <summary>
/// Query para obter o perfil do usuário autenticado
/// </summary>
/// <remarks>
/// Esta query não requer parâmetros pois utiliza o token JWT para identificar o usuário autenticado.
/// Retorna informações completas do perfil incluindo dados pessoais e de auditoria.
/// </remarks>
public class GetUserProfileQuery : IRequest<ApiResponse<GetUserProfileResponse>>
{
    /// <summary>
    /// Inicializa uma nova instância da query GetUserProfile
    /// </summary>
    /// <remarks>
    /// A query não possui parâmetros pois o usuário é identificado automaticamente
    /// através do token JWT presente na requisição HTTP
    /// </remarks>
}