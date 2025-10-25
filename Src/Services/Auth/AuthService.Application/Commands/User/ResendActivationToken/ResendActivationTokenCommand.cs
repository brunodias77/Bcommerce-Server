using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Commands.User.ResendActivationToken;

/// <summary>
/// Comando para reenviar token de ativação de conta
/// </summary>
public class ResendActivationTokenCommand : IRequest<ApiResponse<ResendActivationTokenResponse>>
{
    /// <summary>
    /// Email do usuário que deseja reenviar o token de ativação
    /// </summary>
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;
}