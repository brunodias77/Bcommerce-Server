using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using AuthService.Application.Commands.User.Register;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller responsável por operações de autenticação
/// Fornece endpoints para registro, login e outras operações relacionadas à autenticação
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
        try
        {
            _logger.LogInformation("Iniciando processo de registro para email: {Email}", 
                command?.Email?.Substring(0, Math.Min(command.Email.Length, 3)) + "***");

            // Validação do ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Dados de entrada inválidos para registro");
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<RegisterUserResponse>.Fail(
                    "INVALID_DATA", $"Dados inválidos: {string.Join(", ", errors)}"));
            }

            // Validação adicional do command
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante o registro do usuário");
            
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<RegisterUserResponse>.Fail(
                    "INTERNAL_ERROR", "Erro interno do servidor. Tente novamente mais tarde."));
        }
    }
}