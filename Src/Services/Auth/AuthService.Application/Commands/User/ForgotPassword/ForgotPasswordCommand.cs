using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.Commands.User.ForgotPassword;

/// <summary>
/// Command para solicitação de redefinição de senha
/// Gera um token de redefinição e envia por email
/// </summary>
public class ForgotPasswordCommand : IRequest<ApiResponse<ForgotPasswordResult>>
{
    /// <summary>
    /// Email do usuário que deseja redefinir a senha
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    [StringLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;
}