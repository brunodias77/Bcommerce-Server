using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Commands.User.ActivateAccount;

/// <summary>
/// Command para ativação de conta de usuário através de token de confirmação
/// </summary>
public class ActivateAccountCommand : IRequest<ApiResponse<ActivateAccountResponse>>
{
    /// <summary>
    /// ID do usuário que está sendo ativado
    /// </summary>
    [Required(ErrorMessage = "UserId é obrigatório")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Token de confirmação de email gerado pelo ASP.NET Identity
    /// </summary>
    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;
}