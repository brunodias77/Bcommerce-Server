using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.ResetPassword;

/// <summary>
/// Comando para redefinir a senha do usuário usando token válido
/// </summary>
public class ResetPasswordCommand : IRequest<ApiResponse<ResetPasswordResponse>>
{
    /// <summary>
    /// Email do usuário que está redefinindo a senha
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    [StringLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Token de redefinição de senha gerado pelo sistema
    /// </summary>
    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Nova senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da nova senha
    /// </summary>
    [Required(ErrorMessage = "Confirmação da senha é obrigatória")]
    public string ConfirmPassword { get; set; } = string.Empty;
}