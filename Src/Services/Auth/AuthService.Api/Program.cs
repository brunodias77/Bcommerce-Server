using AuthService.Api.Configurations;
using BuildingBlocks.Middlewares;
using BuildingBlocks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração de CORS para aplicações frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApps", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000") // Angular e React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configuração da camada de infraestrutura
builder.Services.AddInfrastructure(builder.Configuration);

// Configuração da camada de aplicação
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
// 1. CORS - DEVE VIR ANTES de outros middlewares
app.UseCors("AllowFrontendApps");

// 2. HTTPS Redirection - SEMPRE PRIMEIRO para forçar HTTPS
app.UseHttpsRedirection();

// 3. Swagger - apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. BuildingBlocks Middlewares - segurança, validação, monitoramento
app.UseBuildingBlocksMiddleware(app.Environment.IsDevelopment());

// 5. Middleware de autenticação e autorização - APÓS os middlewares de segurança
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configuração dos endpoints de Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();