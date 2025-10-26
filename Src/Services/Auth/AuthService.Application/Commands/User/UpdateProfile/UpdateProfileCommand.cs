using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;

namespace AuthService.Application.Commands.User.UpdateProfile;

/// <summary>
/// Comando para atualizar o perfil do usuário autenticado
/// O usuário é identificado automaticamente através do token JWT
/// </summary>
public class UpdateProfileCommand : IRequest<ApiResponse<UpdateProfileResponse>>
{
    /// <summary>
    /// Nome completo do usuário (opcional)
    /// </summary>
    [MaxLength(255, ErrorMessage = "O nome completo não pode exceder 255 caracteres")]
    public string? FullName { get; set; }

    /// <summary>
    /// Telefone de contato do usuário (opcional)
    /// </summary>
    [MaxLength(20, ErrorMessage = "O telefone não pode exceder 20 caracteres")]
    public string? Phone { get; set; }

    /// <summary>
    /// Data de nascimento do usuário (opcional)
    /// </summary>
    public DateTime? BirthDate { get; set; }
}