using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.DTOs;

/// <summary>
/// DTO para requisição de criação de usuário
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Email do usuário (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter um formato válido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter pelo menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nome completo do usuário (obrigatório)
    /// </summary>
    [Required(ErrorMessage = "Nome completo é obrigatório")]
    [StringLength(100, ErrorMessage = "Nome completo deve ter no máximo 100 caracteres")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Telefone do usuário (opcional)
    /// </summary>
    [Phone(ErrorMessage = "Telefone deve ter um formato válido")]
    public string? Phone { get; set; }
}