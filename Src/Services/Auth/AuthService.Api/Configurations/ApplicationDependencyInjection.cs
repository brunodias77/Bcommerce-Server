using AuthService.Application.Commands.User.Register;
using AuthService.Application.Services;
using AuthService.Domain.Services;
using AuthService.Domain.Services.Token;
using AuthService.Infrastructure.Services;
using AuthService.Api.HealthChecks;
using BuildingBlocks.Mediator;

namespace AuthService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator(configuration);
        services.AddApplicationServices(configuration);
        services.AddApplicationHealthChecks();
        services.AddControllers(); // Direct call to avoid recursion
        services.AddSwagger();
    }

    private static void AddMediator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator(typeof(RegisterUserCommandHandler).Assembly);
    }

    private static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<ITokenJwtService, TokenJwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ILoggedUser, LoggedUser>();
        services.AddHttpContextAccessor();
    }

    private static void AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<IdentityHealthCheck>("identity");
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}