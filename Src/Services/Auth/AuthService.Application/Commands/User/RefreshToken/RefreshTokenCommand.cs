using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Commands.User.RefreshToken;

/// <summary>
/// Comando para renovar tokens JWT usando refresh token
/// </summary>
public class RefreshTokenCommand : IRequest<ApiResponse<RefreshTokenResponse>>
{
    /// <summary>
    /// Token JWT expirado que precisa ser renovado
    /// </summary>
    [Required(ErrorMessage = "Access token é obrigatório")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token válido para renovar o access token
    /// </summary>
    [Required(ErrorMessage = "Refresh token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
}