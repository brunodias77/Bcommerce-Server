using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entidade User personalizada que herda de IdentityUser
/// Contém informações adicionais de perfil do usuário
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Telefone de contato (opcional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Data de nascimento (opcional)
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// Data de criação do registro
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Último login do usuário (opcional)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Relacionamentos com outras entidades
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<SecurityLog> SecurityLogs { get; set; } = new List<SecurityLog>();
}