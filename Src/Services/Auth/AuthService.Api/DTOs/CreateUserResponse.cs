namespace AuthService.Api.DTOs;

/// <summary>
/// DTO para resposta de criação de usuário
/// </summary>
public class CreateUserResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem descritiva do resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID do usuário criado (quando sucesso)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Lista de erros (quando falha)
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
}