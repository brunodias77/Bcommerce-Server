using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.ChangePassword;

/// <summary>
/// Comando para alterar a senha do usuário autenticado
/// O usuário é obtido automaticamente via JWT token através do LoggedUser service
/// </summary>
public class ChangePasswordCommand : IRequest<ApiResponse<ChangePasswordResponse>>
{
    /// <summary>
    /// Senha atual do usuário
    /// </summary>
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nova senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da nova senha
    /// </summary>
    [Required(ErrorMessage = "Confirmação da senha é obrigatória")]
    public string ConfirmPassword { get; set; } = string.Empty;
}