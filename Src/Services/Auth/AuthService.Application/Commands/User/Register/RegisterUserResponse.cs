namespace AuthService.Application.Commands.User.Register;

/// <summary>
/// Resposta do comando de registro de usuário
/// Contém informações sobre o resultado da operação de criação de conta
/// </summary>
public class RegisterUserResponse
{
    /// <summary>
    /// Identificador único do usuário criado
    /// </summary>
    /// <value>GUID do usuário no formato string</value>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário registrado
    /// </summary>
    /// <value>Endereço de email utilizado no registro</value>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mensagem informativa sobre o resultado da operação
    /// </summary>
    /// <value>Mensagem de sucesso ou orientações para o usuário</value>
    public string Message { get; set; } = string.Empty;
}