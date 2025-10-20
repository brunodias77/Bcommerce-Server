using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Configurations;

namespace AuthService.Infrastructure.Data;

/// <summary>
/// Contexto de banco de dados para o serviço de autenticação
/// Herda de IdentityDbContext para incluir as tabelas padrão do Identity
/// </summary>
public class AuthDbContext : IdentityDbContext<User>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    // DbSets para as entidades adicionais
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<SecurityLog> SecurityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Aplicar configurações das entidades
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new SecurityLogConfiguration());
    }
}