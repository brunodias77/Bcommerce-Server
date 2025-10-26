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
    private DateTime? _birthDate;
    public DateTime? BirthDate 
    { 
        get => _birthDate; 
        set => _birthDate = value?.Kind == DateTimeKind.Utc ? value : value?.ToUniversalTime(); 
    }

    /// <summary>
    /// Data de criação do registro
    /// </summary>
    private DateTime _createdAt = DateTime.UtcNow;
    public DateTime CreatedAt 
    { 
        get => _createdAt; 
        set => _createdAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); 
    }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    private DateTime _updatedAt = DateTime.UtcNow;
    public DateTime UpdatedAt 
    { 
        get => _updatedAt; 
        set => _updatedAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); 
    }

    /// <summary>
    /// Último login do usuário (opcional)
    /// </summary>
    private DateTime? _lastLoginAt;
    public DateTime? LastLoginAt 
    { 
        get => _lastLoginAt; 
        set => _lastLoginAt = value?.Kind == DateTimeKind.Utc ? value : value?.ToUniversalTime(); 
    }

    // Relacionamentos com outras entidades
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<SecurityLog> SecurityLogs { get; set; } = new List<SecurityLog>();
}