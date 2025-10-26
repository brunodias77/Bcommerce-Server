namespace AuthService.Domain.Entities;

/// <summary>
/// Entidade para registrar eventos de segurança (login, falhas, etc.)
/// </summary>
public class SecurityLog
{
    /// <summary>
    /// Identificador do log
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Usuário relacionado ao evento (opcional)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Tipo de evento (ex: LOGIN_SUCCESS, LOGIN_FAILED)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Endereço IP do evento
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Detalhes adicionais
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Data de criação do log
    /// </summary>
    private DateTime _createdAt = DateTime.UtcNow;
    public DateTime CreatedAt 
    { 
        get => _createdAt; 
        set => _createdAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(); 
    }

    // Relacionamento com User (opcional)
    public virtual User? User { get; set; }
}