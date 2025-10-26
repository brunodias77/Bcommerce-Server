using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using AuthService.Application.Commands.User.Register;
using AuthService.Application.Commands.User.ActivateAccount;
using AuthService.Application.Commands.User.ResendActivationToken;
using AuthService.Application.Commands.User.Login;
using AuthService.Application.Commands.User.ChangePassword;
using AuthService.Application.Commands.User.ForgotPassword;
using AuthService.Application.Commands.User.Logout;
using AuthService.Application.Commands.User.RefreshToken;
using AuthService.Application.Commands.User.ResetPassword;
using AuthService.Application.Commands.User.UpdateProfile;
using AuthService.Application.Queries.Users.GetUserProfile;

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

    /// <summary>
    /// Obtém o perfil do usuário autenticado
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados do perfil do usuário</returns>
    /// <response code="200">Perfil obtido com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetUserProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GetUserProfileResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GetUserProfileResponse>>> GetProfile(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando busca do perfil do usuário autenticado");

        var query = new GetUserProfileQuery();
        var result = await _mediator.SendAsync<ApiResponse<GetUserProfileResponse>>(query, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Perfil obtido com sucesso para usuário: {UserId}", result.Data?.UserId);
            return Ok(result);
        }

        _logger.LogWarning("Falha ao obter perfil do usuário: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Altera a senha do usuário autenticado
    /// </summary>
    /// <param name="command">Dados contendo a senha atual e nova senha</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados da alteração de senha</returns>
    /// <response code="200">Senha alterada com sucesso</response>
    /// <response code="400">Dados inválidos ou senha atual incorreta</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ChangePasswordResponse>>> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de alteração de senha para usuário autenticado");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de alteração de senha é nulo");
            return BadRequest(ApiResponse<ChangePasswordResponse>.Fail(
                "MISSING_DATA", "Dados de alteração de senha são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<ChangePasswordResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Senha alterada com sucesso para usuário autenticado");
            return Ok(result);
        }

        // Se chegou aqui, houve erro na alteração
        _logger.LogWarning("Falha na alteração de senha: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Solicita redefinição de senha através do email
    /// </summary>
    /// <param name="command">Dados contendo o email do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados da solicitação</returns>
    /// <response code="200">Solicitação processada com sucesso</response>
    /// <response code="400">Email inválido ou não encontrado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResult>>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de esqueci minha senha para email: {Email}", 
            command?.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de esqueci minha senha é nulo");
            return BadRequest(ApiResponse<ForgotPasswordResult>.Fail(
                "MISSING_DATA", "Dados são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<ForgotPasswordResult>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Solicitação de redefinição de senha processada com sucesso para: {Email}", 
                command.Email?.Substring(0, Math.Min(command.Email.Length, 3)) + "***");
            return Ok(result);
        }

        // Se chegou aqui, houve erro na solicitação
        _logger.LogWarning("Falha na solicitação de redefinição de senha para {Email}: {Errors}", 
            command.Email?.Substring(0, Math.Min(command.Email?.Length ?? 0, 3)) + "***", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Realiza logout do usuário autenticado
    /// </summary>
    /// <param name="command">Dados do logout (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados do logout</returns>
    /// <response code="200">Logout realizado com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LogoutUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LogoutUserResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LogoutUserResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LogoutUserResponse>>> Logout(
        [FromBody] LogoutUserCommand? command = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de logout para usuário autenticado");

        // Se command é nulo, criar um vazio
        command ??= new LogoutUserCommand();

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<LogoutUserResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Logout realizado com sucesso para usuário autenticado");
            return Ok(result);
        }

        // Se chegou aqui, houve erro no logout
        _logger.LogWarning("Falha no logout: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Renova o token de acesso usando o refresh token
    /// </summary>
    /// <param name="command">Dados contendo o refresh token</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com novos tokens</returns>
    /// <response code="200">Token renovado com sucesso</response>
    /// <response code="400">Refresh token inválido ou expirado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de renovação de token");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de renovação de token é nulo");
            return BadRequest(ApiResponse<RefreshTokenResponse>.Fail(
                "MISSING_DATA", "Dados de renovação são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<RefreshTokenResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Token renovado com sucesso");
            return Ok(result);
        }

        // Se chegou aqui, houve erro na renovação
        _logger.LogWarning("Falha na renovação de token: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Redefine a senha do usuário usando token válido
    /// </summary>
    /// <param name="command">Dados contendo token e nova senha</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados da redefinição</returns>
    /// <response code="200">Senha redefinida com sucesso</response>
    /// <response code="400">Token inválido ou dados incorretos</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ResetPasswordResponse>>> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de redefinição de senha");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de redefinição de senha é nulo");
            return BadRequest(ApiResponse<ResetPasswordResponse>.Fail(
                "MISSING_DATA", "Dados de redefinição são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<ResetPasswordResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Senha redefinida com sucesso");
            return Ok(result);
        }

        // Se chegou aqui, houve erro na redefinição
        _logger.LogWarning("Falha na redefinição de senha: {Errors}", result.Errors);
        return BadRequest(result);
    }

    /// <summary>
    /// Atualiza o perfil do usuário autenticado
    /// </summary>
    /// <param name="command">Dados do perfil a serem atualizados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta com dados da atualização</returns>
    /// <response code="200">Perfil atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UpdateProfileResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UpdateProfileResponse>>> UpdateProfile(
        [FromBody] UpdateProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando processo de atualização de perfil para usuário autenticado");

        // Validação básica do command
        if (command == null)
        {
            _logger.LogWarning("Command de atualização de perfil é nulo");
            return BadRequest(ApiResponse<UpdateProfileResponse>.Fail(
                "MISSING_DATA", "Dados de atualização são obrigatórios"));
        }

        // Processa o command através do Mediator
        var result = await _mediator.SendAsync<ApiResponse<UpdateProfileResponse>>(command, cancellationToken);

        // Verifica se o resultado foi bem-sucedido
        if (result.Success)
        {
            _logger.LogInformation("Perfil atualizado com sucesso para usuário autenticado");
            return Ok(result);
        }

        // Se chegou aqui, houve erro na atualização
        _logger.LogWarning("Falha na atualização de perfil: {Errors}", result.Errors);
        return BadRequest(result);
    }
}