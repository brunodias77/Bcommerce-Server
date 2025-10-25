using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.Login;

/// <summary>
/// Command para realizar login de usuário
/// </summary>
public class LoginUserCommand : IRequest<ApiResponse<LoginUserResponse>>
{
    /// <summary>
    /// Email do usuário
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    [StringLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Senha deve ter pelo menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;
}