using AuthService.Api.DTOs;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de usuários
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserController> _logger;

    public UserController(UserManager<User> userManager, ILogger<UserController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para criar um novo usuário
    /// </summary>
    /// <param name="request">Dados do usuário a ser criado</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            _logger.LogInformation("Iniciando criação de usuário: {Email}", request.Email);

            // Verificar se o usuário já existe
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", request.Email);
                return BadRequest(new CreateUserResponse
                {
                    Success = false,
                    Message = "Usuário já existe",
                    Errors = new List<string> { "Email já está em uso" }
                });
            }

            // Criar o novo usuário
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("Usuário criado com sucesso: {Email}, ID: {UserId}", request.Email, user.Id);
                return Ok(new CreateUserResponse
                {
                    Success = true,
                    Message = "Usuário criado com sucesso",
                    UserId = user.Id
                });
            }

            // Caso haja erros na criação
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Falha ao criar usuário: {Email}, Erros: {Errors}", 
                request.Email, string.Join(", ", errors));
            
            return BadRequest(new CreateUserResponse
            {
                Success = false,
                Message = "Falha ao criar usuário",
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new CreateUserResponse
            {
                Success = false,
                Message = "Erro interno ao processar a solicitação",
                Errors = new List<string> { "Ocorreu um erro ao processar a solicitação" }
            });
        }
    }
}