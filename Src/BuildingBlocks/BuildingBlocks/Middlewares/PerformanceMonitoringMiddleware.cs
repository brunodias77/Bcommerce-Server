using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Middlewares
{
    /// <summary>
    /// Middleware para monitoramento de performance de requisições HTTP.
    /// 
    /// Este middleware mede o tempo de execução de cada requisição e identifica
    /// requisições que excedem um limite de tempo configurado, ajudando a detectar
    /// gargalos de performance na aplicação.
    /// 
    /// Funcionalidades principais:
    /// - Mede o tempo total de processamento de cada requisição
    /// - Identifica e registra requisições lentas (acima do threshold configurado)
    /// - Fornece métricas detalhadas incluindo path, método HTTP, duração e status code
    /// - Utiliza Stopwatch para medição precisa de tempo
    /// - Garante que o monitoramento ocorra mesmo em caso de exceções
    /// 
    /// Benefícios:
    /// - Detecção proativa de problemas de performance
    /// - Identificação de endpoints que precisam de otimização
    /// - Monitoramento contínuo da saúde da aplicação
    /// - Dados para análise de SLA e métricas de performance
    /// 
    /// Uso: Registre este middleware no início do pipeline para capturar
    /// o tempo total de processamento de todas as requisições.
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        
        /// <summary>
        /// Limite de tempo em milissegundos para considerar uma requisição como lenta.
        /// Requisições que excedem este valor serão registradas como advertências.
        /// 
        /// Valor padrão: 1000ms (1 segundo)
        /// 
        /// Nota: Em produção, considere ajustar este valor baseado nos SLAs
        /// e características específicas da sua aplicação.
        /// </summary>
        private const int SlowRequestThresholdMs = 1000; // 1 segundo

        /// <summary>
        /// Construtor do middleware de monitoramento de performance.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar métricas de performance</param>
        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método:
        /// 1. Inicia um cronômetro (Stopwatch) antes do processamento
        /// 2. Executa o próximo middleware no pipeline
        /// 3. Para o cronômetro após o processamento (mesmo em caso de exceção)
        /// 4. Verifica se o tempo excede o threshold configurado
        /// 5. Registra um log de advertência para requisições lentas
        /// 
        /// O uso do bloco try/finally garante que o monitoramento seja realizado
        /// independentemente de exceções que possam ocorrer durante o processamento.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Inicia o cronômetro para medir o tempo de processamento
            // Stopwatch fornece medição de alta precisão
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Executa o próximo middleware no pipeline
                // Todo o processamento da requisição acontece aqui
                await _next(context);
            }
            finally
            {
                // Para o cronômetro independentemente de sucesso ou falha
                // O bloco finally garante que o monitoramento sempre ocorra
                stopwatch.Stop();

                // Verifica se a requisição excedeu o limite de tempo configurado
                if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
                {
                    // Registra um log de advertência com informações detalhadas
                    // O emoji 🐌 facilita a identificação visual de requisições lentas
                    _logger.LogWarning(
                        "🐌 SLOW REQUEST | Path: {Path} | Method: {Method} | " +
                        "Duration: {Duration}ms | StatusCode: {StatusCode}",
                        context.Request.Path,           // Endpoint acessado
                        context.Request.Method,         // Método HTTP (GET, POST, etc.)
                        stopwatch.ElapsedMilliseconds,  // Tempo total em milissegundos
                        context.Response.StatusCode);   // Código de status da resposta
                }
            }
        }
    }
}
