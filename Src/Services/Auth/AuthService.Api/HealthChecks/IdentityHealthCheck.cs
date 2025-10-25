using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using AuthService.Domain.Entities;

namespace AuthService.Api.HealthChecks;

/// <summary>
/// Health Check personalizado para verificar o status do ASP.NET Core Identity
/// </summary>
public class IdentityHealthCheck : IHealthCheck
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<IdentityHealthCheck> _logger;

    public IdentityHealthCheck(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<IdentityHealthCheck> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executando verificação de saúde do Identity");

            // Verifica se o UserManager está funcionando
            var userCount = _userManager.Users.Count();
            
            // Verifica se o RoleManager está funcionando
            var roleCount = _roleManager.Roles.Count();

            var data = new Dictionary<string, object>
            {
                ["user_count"] = userCount,
                ["role_count"] = roleCount,
                ["timestamp"] = DateTime.UtcNow
            };

            _logger.LogInformation("Identity Health Check executado com sucesso. Usuários: {UserCount}, Roles: {RoleCount}", 
                userCount, roleCount);

            return HealthCheckResult.Healthy("Identity está funcionando corretamente", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante a verificação de saúde do Identity");
            
            return HealthCheckResult.Unhealthy(
                "Erro ao verificar o Identity", 
                ex, 
                new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                });
        }
    }
}