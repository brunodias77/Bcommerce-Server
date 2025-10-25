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
    /// Middleware para monitoramento e logging de validações de modelo.
    /// 
    /// Este middleware atua como um sistema de monitoramento para validações que ocorrem
    /// automaticamente no ASP.NET Core através de DataAnnotations e ModelState.
    /// 
    /// Funcionalidades principais:
    /// - Monitora respostas com status 400 (Bad Request) que indicam falhas de validação
    /// - Registra logs de advertência quando validações falham
    /// - Fornece visibilidade sobre problemas de validação na aplicação
    /// - Atua como fallback para capturar validações não tratadas explicitamente
    /// 
    /// Nota: Este middleware não realiza validação ativa, mas sim monitora os resultados
    /// das validações automáticas do ASP.NET Core. A validação real é feita pelos
    /// controllers através do ModelState e DataAnnotations.
    /// 
    /// Uso: Registre este middleware após middlewares de roteamento para capturar
    /// adequadamente os códigos de status de validação.
    /// </summary>
    public class ModelValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ModelValidationMiddleware> _logger;

        /// <summary>
        /// Construtor do middleware de monitoramento de validação de modelo.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar eventos de validação</param>
        public ModelValidationMiddleware(
            RequestDelegate next,
            ILogger<ModelValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método:
        /// 1. Permite que a requisição continue através do pipeline
        /// 2. Monitora o código de status da resposta após o processamento
        /// 3. Registra logs quando detecta falhas de validação (status 400)
        /// 
        /// O middleware atua de forma passiva, observando os resultados das validações
        /// que já foram processadas pelos controllers e outros componentes.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // ModelState é validado automaticamente pelos controllers do ASP.NET Core
            // Este middleware serve como sistema de monitoramento e logging
            // A validação real acontece através de:
            // - DataAnnotations nos modelos
            // - ModelState.IsValid nos controllers
            // - Filtros de validação automática

            // Executa o próximo middleware no pipeline
            await _next(context);

            // Após o processamento da requisição, verifica se houve falha de validação
            // Status 400 (Bad Request) é o código padrão para erros de validação
            if (context.Response.StatusCode == 400)
            {
                // Registra um log de advertência para monitoramento
                // O emoji ⚠️ facilita a identificação visual nos logs
                _logger.LogWarning(
                    "⚠️ Validation failed | Path: {Path} | Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);
            }
        }
    }
}
