using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthService.Domain.Entities;

namespace AuthService.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de Health Checks da aplicação
/// Fornece informações sobre o status da aplicação, banco de dados e serviços
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly AuthDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        AuthDbContext dbContext,
        UserManager<User> userManager,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint básico de health check - retorna status geral da aplicação
    /// </summary>
    /// <returns>Status geral da aplicação</returns>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            _logger.LogInformation("Executando health check básico");

            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                application = "AuthService.Api"
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            
            _logger.LogInformation("Health check básico executado. Status: {Status}", healthReport.Status);
            
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante health check básico");
            
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                application = "AuthService.Api",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Endpoint detalhado de health check - retorna informações completas sobre todos os serviços
    /// </summary>
    /// <returns>Informações detalhadas sobre o status de todos os componentes</returns>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            _logger.LogInformation("Executando health check detalhado");

            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTime.UtcNow,
                application = "AuthService.Api",
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                totalDuration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration.TotalMilliseconds,
                    data = entry.Value.Data,
                    exception = entry.Value.Exception?.Message
                }).ToList()
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            
            _logger.LogInformation("Health check detalhado executado. Status: {Status}, Duração: {Duration}ms", 
                healthReport.Status, healthReport.TotalDuration.TotalMilliseconds);
            
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante health check detalhado");
            
            return StatusCode(503, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                application = "AuthService.Api",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Endpoint específico para verificação do banco de dados
    /// </summary>
    /// <returns>Status específico do banco de dados</returns>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            _logger.LogInformation("Executando health check do banco de dados");

            // Testa conexão com o banco
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                _logger.LogWarning("Não foi possível conectar ao banco de dados");
                
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    component = "Database",
                    timestamp = DateTime.UtcNow,
                    message = "Não foi possível conectar ao banco de dados"
                });
            }

            // Obtém informações adicionais do banco
            var userCount = await _dbContext.Users.CountAsync();
            
            var response = new
            {
                status = "Healthy",
                component = "Database",
                timestamp = DateTime.UtcNow,
                message = "Banco de dados está funcionando corretamente",
                data = new
                {
                    connection_status = "Connected",
                    user_count = userCount,
                    database_provider = _dbContext.Database.ProviderName
                }
            };

            _logger.LogInformation("Health check do banco executado com sucesso. Usuários: {UserCount}", userCount);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante health check do banco de dados");
            
            return StatusCode(503, new
            {
                status = "Unhealthy",
                component = "Database",
                timestamp = DateTime.UtcNow,
                message = "Erro ao verificar o banco de dados",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Endpoint específico para verificação do ASP.NET Core Identity
    /// </summary>
    /// <returns>Status específico do Identity</returns>
    [HttpGet("identity")]
    public async Task<IActionResult> GetIdentityHealth()
    {
        try
        {
            _logger.LogInformation("Executando health check do Identity");

            // Verifica se o UserManager está funcionando
            var userCount = _userManager.Users.Count();
            
            // Testa uma operação básica do Identity
            var testUser = await _userManager.FindByEmailAsync("test@nonexistent.com");
            
            var response = new
            {
                status = "Healthy",
                component = "Identity",
                timestamp = DateTime.UtcNow,
                message = "ASP.NET Core Identity está funcionando corretamente",
                data = new
                {
                    user_manager_status = "Working",
                    user_count = userCount,
                    test_query_executed = true
                }
            };

            _logger.LogInformation("Health check do Identity executado com sucesso. Usuários: {UserCount}", userCount);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante health check do Identity");
            
            return StatusCode(503, new
            {
                status = "Unhealthy",
                component = "Identity",
                timestamp = DateTime.UtcNow,
                message = "Erro ao verificar o ASP.NET Core Identity",
                error = ex.Message
            });
        }
    }
}