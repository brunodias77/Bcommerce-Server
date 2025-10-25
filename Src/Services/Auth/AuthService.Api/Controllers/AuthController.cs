using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using AuthService.Application.Commands.User.Register;
using AuthService.Application.Commands.User.ActivateAccount;
using AuthService.Application.Commands.User.ResendActivationToken;
using AuthService.Application.Commands.User.Login;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller responsável por operações de autenticação
/// Fornece endpoints para registro, login e outras operações relacionadas à autenticação
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Construtor que recebe as dependências via injeção
    /// </summary>
    /// <param name="mediator">Mediator para processamento de commands e queries</param>
    /// <param name="logger">Logger para registro de operações</param>
    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="command">Dados do usuário para registro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados do usuário criado</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos ou email já existe</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de registro para email: {Email}", 
            command?.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***");

        // Validação básica do command (apenas nulidade)
        if (command == null)
        {
            _logger.LogWarning("Command de registro é nulo");
            return BadRequest(ApiResponse<RegisterUserResponse>.Fail(
                "MISSING_DATA", "Dados do usuário são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<RegisterUserResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Usuário registrado com sucesso: {UserId}", result.Data?.UserId);
            return CreatedAtAction(nameof(Register), new { id = result.Data?.UserId }, result);
        }

        // Se chegou aqui, houve erro de validação
        _logger.LogWarning("Falha no registro do usuário: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Ativa uma conta de usuário através do token de confirmação de email
    /// </summary>
    /// <param name="token">Token de confirmação de email</param>
    /// <param name="userId">ID do usuário a ser ativado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados da ativação</returns>
    /// <response code="200">Conta ativada com sucesso</response>
    /// <response code="400">Token inválido ou usuário não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<ActivateAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ActivateAccountResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ActivateAccountResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ActivateAccountResponse>>> ConfirmEmail(
        [FromQuery] string token,
        [FromQuery] string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de ativação de conta para usuário: {UserId}", userId);

        // Validação dos parâmetros
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token de confirmação não fornecido");
            return BadRequest(ApiResponse<ActivateAccountResponse>.Fail(
                "MISSING_TOKEN", "Token de confirmação é obrigatório"));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("UserId não fornecido");
            return BadRequest(ApiResponse<ActivateAccountResponse>.Fail(
                "MISSING_USERID", "ID do usuário é obrigatório"));
        }

        // Validar formato do GUID
        if (!Guid.TryParse(userId, out _))
        {
            _logger.LogWarning("UserId com formato inválido: {UserId}", userId);
            return BadRequest(ApiResponse<ActivateAccountResponse>.Fail(
                "INVALID_USERID", "ID do usuário deve ser um GUID válido"));
        }

        // Criar o command
        var command = new ActivateAccountCommand
        {
            Token = token,
            UserId = userId
        };

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<ActivateAccountResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Conta ativada com sucesso para usuário: {UserId}", userId);
            return Ok(result);
        }

        // Se chegou aqui, houve erro na ativação
        _logger.LogWarning("Falha na ativação da conta do usuário {UserId}: {Errors}", userId, result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Reenvia o token de ativação para o email do usuário
    /// </summary>
    /// <param name="command">Dados contendo o email do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados do reenvio</returns>
    /// <response code="200">Token reenviado com sucesso</response>
    /// <response code="400">Email inválido ou usuário não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("resend-activation-token")]
    [ProducesResponseType(typeof(ApiResponse<ResendActivationTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResendActivationTokenResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ResendActivationTokenResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ResendActivationTokenResponse>>> ResendActivationToken(
        [FromBody] ResendActivationTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de reenvio de token de ativação para email: {Email}", 
            command?.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de reenvio de token é nulo");
            return BadRequest(ApiResponse<ResendActivationTokenResponse>.Fail(
                "MISSING_DATA", "Dados são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<ResendActivationTokenResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Token de ativação reenviado com sucesso para: {Email}", 
                command.Email?.Substring(0, Math.Min(command.Email.Length, 3)) + "***");
            return Ok(result);
        }

        // Se chegou aqui, houve erro no reenvio
        _logger.LogWarning("Falha no reenvio de token para {Email}: {Errors}", 
            command.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Autentica um usuário no sistema
    /// </summary>
    /// <param name="command">Dados de login do usuário (email e senha)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com tokens de acesso e dados do usuário</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Credenciais inválidas ou dados incorretos</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginUserResponse>>> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de login para email: {Email}", 
            command?.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de login é nulo");
            return BadRequest(ApiResponse<LoginUserResponse>.Fail(
                "MISSING_DATA", "Dados de login são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<LoginUserResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Login realizado com sucesso para: {Email}", 
                command.Email?.Substring(0, Math.Min(command.Email.Length, 3)) + "***");
            return Ok(result);
        }

        // Se chegou aqui, houve erro no login
        _logger.LogWarning("Falha no login para {Email}: {Errors}", 
            command.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***", result.Errors);
        return BadRequest(result);
    }
}