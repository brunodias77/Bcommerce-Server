using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using AuthService.Domain.Entities;
using AuthService.Domain.Services;

namespace AuthService.Application.Services;

/// <summary>
/// Serviço responsável por obter informações do usuário autenticado atual
/// Utiliza o contexto HTTP e JWT claims para identificar e buscar o usuário
/// </summary>
public class LoggedUser : ILoggedUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// Construtor que recebe as dependências necessárias via injeção
    /// </summary>
    /// <param name="httpContextAccessor">Accessor para obter o contexto HTTP atual</param>
    /// <param name="userManager">Gerenciador de usuários do Identity</param>
    public LoggedUser(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>
    /// Obtém o usuário autenticado atual
    /// Extrai o ID do usuário dos claims JWT e busca os dados completos no banco
    /// </summary>
    /// <returns>Usuário autenticado atual</returns>
    /// <exception cref="UnauthorizedAccessException">Quando não há contexto HTTP ou usuário não autenticado</exception>
    /// <exception cref="InvalidOperationException">Quando o usuário não é encontrado no banco de dados</exception>
    public async Task<User> User()
    {
        // Obter o contexto HTTP atual
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new UnauthorizedAccessException("Contexto HTTP não disponível");
        }

        // Obter o ClaimsPrincipal do usuário autenticado
        var claimsPrincipal = httpContext.User;
        if (claimsPrincipal == null || !claimsPrincipal.Identity?.IsAuthenticated == true)
        {
            throw new UnauthorizedAccessException("Usuário não autenticado");
        }

        // Extrair o ID do usuário dos claims JWT
        // Tenta primeiro ClaimTypes.NameIdentifier, depois JwtRegisteredClaimNames.Sub
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("ID do usuário não encontrado nos claims JWT");
        }

        // Buscar o usuário completo no banco de dados
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"Usuário com ID '{userId}' não encontrado no banco de dados");
        }

        return user;
    }
}