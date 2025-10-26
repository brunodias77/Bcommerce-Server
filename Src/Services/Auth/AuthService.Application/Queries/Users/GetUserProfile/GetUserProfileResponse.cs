namespace AuthService.Application.Queries.Users.GetUserProfile;

/// <summary>
/// Resposta da query GetUserProfile contendo informações do perfil do usuário
/// </summary>
/// <param name="UserId">Identificador único do usuário</param>
/// <param name="Email">Endereço de email do usuário</param>
/// <param name="FullName">Nome completo do usuário</param>
/// <param name="Phone">Número de telefone do usuário</param>
/// <param name="BirthDate">Data de nascimento do usuário (opcional)</param>
/// <param name="CreatedAt">Data e hora de criação da conta</param>
/// <param name="UpdatedAt">Data e hora da última atualização do perfil</param>
/// <param name="LastLoginAt">Data e hora do último login (opcional)</param>
/// <param name="EmailConfirmed">Indica se o email foi confirmado</param>
public record GetUserProfileResponse(
    string UserId,
    string Email,
    string FullName,
    string Phone,
    DateTime? BirthDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt,
    bool EmailConfirmed
);
