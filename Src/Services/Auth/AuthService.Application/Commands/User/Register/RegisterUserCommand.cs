using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.Register;

/// <summary>
/// Command para registro de novo usuário no sistema
/// </summary>
public class RegisterUserCommand : IRequest<ApiResponse<RegisterUserResponse>>
{
    /// <summary>
    /// Email do usuário (obrigatório e único)
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    [StringLength(256, ErrorMessage = "Email deve ter no máximo 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário (obrigatória)
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Senha deve ter pelo menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Nome completo é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Telefone do usuário (opcional)
    /// </summary>
    [Phone(ErrorMessage = "Telefone deve ter um formato válido")]
    public string? Phone { get; set; }

    /// <summary>
    /// Data de nascimento do usuário (opcional)
    /// </summary>
    public DateTime? BirthDate { get; set; }
}