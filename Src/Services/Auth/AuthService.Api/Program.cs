using AuthService.Application.Commands.User.ActivateAccount;
using AuthService.Application.Commands.User.Register;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Services;
using AuthService.Domain.Services.Token;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using AuthService.Api.HealthChecks;
using BuildingBlocks.Data;
using BuildingBlocks.Mediator;
using BuildingBlocks.Extensions;
using BuildingBlocks.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configuração do Entity Framework com PostgreSQL
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração do ASP.NET Core Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
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

// Configuração do Mediator
builder.Services.AddMediator(typeof(RegisterUserCommandHandler).Assembly);

// Configuração dos serviços de aplicação
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<ITokenJwtService, TokenJwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Configuração dos Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuthDbContext>("database")
    .AddCheck<IdentityHealthCheck>("identity");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// 1. HTTPS Redirection - SEMPRE PRIMEIRO para forçar HTTPS
app.UseHttpsRedirection();

// 2. Swagger - apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. BuildingBlocks Middlewares - segurança, validação, monitoramento
app.UseBuildingBlocksMiddleware(app.Environment.IsDevelopment());

// 4. Middleware de autenticação e autorização - APÓS os middlewares de segurança
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configuração dos endpoints de Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();