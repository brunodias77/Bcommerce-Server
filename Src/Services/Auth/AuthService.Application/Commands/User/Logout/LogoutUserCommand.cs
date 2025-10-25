using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.Logout;

/// <summary>
/// Comando para realizar logout de usuário
/// Revoga refresh tokens específicos ou todos os tokens ativos do usuário
/// </summary>
/// <remarks>
/// Este comando não requer parâmetros de identificação do usuário pois utiliza o token JWT 
/// para identificar automaticamente o usuário autenticado que está fazendo logout.
/// </remarks>
public class LogoutUserCommand : IRequest<ApiResponse<LogoutUserResponse>>
{
    /// <summary>
    /// Refresh token específico para revogar (opcional)
    /// Se não fornecido, todos os refresh tokens ativos do usuário serão revogados
    /// </summary>
    /// <remarks>
    /// O usuário é identificado automaticamente através do token JWT presente na requisição HTTP.
    /// Não é necessário fornecer o UserId pois ele é extraído do contexto de autenticação.
    /// </remarks>
    public string? RefreshToken { get; set; }
}