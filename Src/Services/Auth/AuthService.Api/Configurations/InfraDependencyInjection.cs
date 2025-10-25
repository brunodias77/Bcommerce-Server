using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using AuthService.Api.HealthChecks;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Repositories;
using BuildingBlocks.Data;
using BuildingBlocks.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddIdentity(services);
        AddUnitOfWork(services);
        AddInfrastructureHealthChecks(services);
        AddRepositories(services);
    }

    /// <summary>
    /// Configura o Entity Framework com PostgreSQL
    /// </summary>
    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    }

    /// <summary>
    /// Configura o ASP.NET Core Identity
    /// </summary>
    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole>(options =>
        {
            // Configurações de senha
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Configurações de lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Configurações de usuário
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            // Configurações de confirmação de email
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<PortugueseIdentityErrorDescriber>();
    }

    /// <summary>
    /// Configura o UnitOfWork
    /// </summary>
    private static void AddUnitOfWork(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    /// <summary>
    /// Configura os Health Checks para infraestrutura
    /// </summary>
    private static void AddInfrastructureHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AuthDbContext>("database");
    }

    // TODO: crie o metodo para adicionar os repositories
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccountTokenRepository, AccountTokenRepository>();
    }
}