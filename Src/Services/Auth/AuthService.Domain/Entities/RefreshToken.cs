namespace AuthService.Domain.Entities;

/// <summary>
/// Entidade para armazenar tokens de atualização (para renovar JWTs)
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Identificador único do token
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID do usuário associado ao token
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Valor do token de atualização
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração
    /// </summary>
    private DateTime _expiresAt;
    public DateTime ExpiresAt 
    { 
        get => _expiresAt; 
        set => _expiresAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); 
    }

    /// <summary>
    /// Data de criação
    /// </summary>
    private DateTime _createdAt = DateTime.UtcNow;
    public DateTime CreatedAt 
    { 
        get => _createdAt; 
        set => _createdAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); 
    }

    /// <summary>
    /// Data de revogação (caso o token seja invalidado)
    /// </summary>
    private DateTime? _revokedAt;
    public DateTime? RevokedAt 
    { 
        get => _revokedAt; 
        set => _revokedAt = value?.Kind == DateTimeKind.Utc ? value : value?.ToUniversalTime(); 
    }

    /// <summary>
    /// Campo calculado que indica se o token está ativo
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    // Relacionamento com User
    public virtual User User { get; set; } = null!;
}