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
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data de revogação (caso o token seja invalidado)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Campo calculado que indica se o token está ativo
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    // Relacionamento com User
    public virtual User User { get; set; } = null!;
}