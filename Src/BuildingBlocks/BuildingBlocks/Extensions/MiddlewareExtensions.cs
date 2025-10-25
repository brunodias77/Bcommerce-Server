using BuildingBlocks.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Extensions
{
    /// <summary>
    /// Extensões para configuração dos middlewares do BuildingBlocks
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Configura todos os middlewares do BuildingBlocks na ordem correta para máxima eficiência e segurança.
        /// 
        /// Ordem dos middlewares (crítica para funcionamento correto):
        /// 1. SecurityHeadersMiddleware - Adiciona cabeçalhos de segurança (sempre primeiro)
        /// 2. ExceptionHandlingMiddleware - Captura exceções globalmente
        /// 3. RequestValidationMiddleware - Valida requisições antes do processamento
        /// 4. RateLimitingMiddleware - Controla taxa de requisições
        /// 5. PerformanceMonitoringMiddleware - Monitora performance das requisições
        /// 6. RequestResponseLoggingMiddleware - Log detalhado (apenas em desenvolvimento)
        /// 7. ModelValidationMiddleware - Monitora validações de modelo
        /// 
        /// A ordem é importante porque:
        /// - Segurança deve ser aplicada primeiro
        /// - Exceções devem ser capturadas o mais cedo possível
        /// - Validações devem ocorrer antes do processamento
        /// - Monitoramento deve envolver todo o pipeline
        /// </summary>
        /// <param name="app">Instância do WebApplication</param>
        /// <param name="isDevelopment">Indica se está em ambiente de desenvolvimento</param>
        /// <returns>WebApplication para permitir method chaining</returns>
        public static WebApplication UseBuildingBlocksMiddleware(this WebApplication app, bool isDevelopment = false)
        {
            // 1. SecurityHeadersMiddleware - SEMPRE PRIMEIRO
            // Adiciona cabeçalhos de segurança HTTP recomendados pela OWASP
            // Deve ser o primeiro para garantir que todas as respostas tenham os cabeçalhos
            app.UseMiddleware<SecurityHeadersMiddleware>();

            // 2. ExceptionHandlingMiddleware - SEGUNDO
            // Captura todas as exceções não tratadas e converte em respostas JSON padronizadas
            // Deve estar cedo no pipeline para capturar exceções de outros middlewares
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // 3. RequestValidationMiddleware - TERCEIRO
            // Valida Content-Type e tamanho das requisições
            // Rejeita requisições inválidas antes do processamento pesado
            app.UseMiddleware<RequestValidationMiddleware>();

            // 4. RateLimitingMiddleware - QUARTO
            // Controla taxa de requisições por IP
            // Deve estar antes do processamento para economizar recursos
            app.UseMiddleware<RateLimitingMiddleware>();

            // 5. PerformanceMonitoringMiddleware - QUINTO
            // Monitora tempo de execução das requisições
            // Deve envolver todo o processamento subsequente
            app.UseMiddleware<PerformanceMonitoringMiddleware>();

            // 6. RequestResponseLoggingMiddleware - APENAS EM DESENVOLVIMENTO
            // Log detalhado de requisições e respostas
            // Pode impactar performance, então apenas em desenvolvimento
            if (isDevelopment)
            {
                app.UseMiddleware<RequestResponseLoggingMiddleware>();
            }

            // 7. ModelValidationMiddleware - ÚLTIMO DOS NOSSOS MIDDLEWARES
            // Monitora validações de modelo que ocorrem nos controllers
            // Deve estar próximo aos controllers para capturar validações
            app.UseMiddleware<ModelValidationMiddleware>();

            return app;
        }
    }
}
