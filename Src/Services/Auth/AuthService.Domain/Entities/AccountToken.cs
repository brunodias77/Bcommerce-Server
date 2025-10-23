using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Entidade que representa tokens de conta (ativação e redefinição de senha)
/// Armazena tokens temporários para operações de segurança
/// </summary>
public class AccountToken
{
    /// <summary>
    /// Identificador único do token
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID do usuário vinculado ao token (referência ao AspNetUsers.Id)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Valor único do token (ex: hash SHA256)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tipo do token (ACCOUNT_ACTIVATION / PASSWORD_RESET)
    /// </summary>
    public AccountTokenType TokenType { get; set; }

    /// <summary>
    /// Data e hora de expiração do token
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Data e hora em que o token foi utilizado (opcional)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Data e hora em que o token foi revogado (opcional)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Data de criação do registro
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica se o token está ativo (não usado, não revogado e não expirado)
    /// Campo calculado baseado em UsedAt, RevokedAt e ExpiresAt
    /// </summary>
    public bool IsActive => 
        UsedAt == null && 
        RevokedAt == null && 
        ExpiresAt > DateTime.UtcNow;

    // Relacionamentos
    /// <summary>
    /// Usuário vinculado ao token
    /// </summary>
    public virtual User User { get; set; } = null!;
}