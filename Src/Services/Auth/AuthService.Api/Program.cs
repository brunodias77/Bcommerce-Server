using AuthService.Api.Configurations;
using BuildingBlocks.Middlewares;
using BuildingBlocks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configuração da camada de infraestrutura
builder.Services.AddInfrastructure(builder.Configuration);

// Configuração da camada de aplicação
builder.Services.AddApplication(builder.Configuration);

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