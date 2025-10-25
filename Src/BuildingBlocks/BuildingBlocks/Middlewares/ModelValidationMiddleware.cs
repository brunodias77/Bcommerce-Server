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
    /// Valida automaticamente DataAnnotations dos models
    /// </summary>
    public class ModelValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ModelValidationMiddleware> _logger;

        public ModelValidationMiddleware(
            RequestDelegate next,
            ILogger<ModelValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ModelState é validado automaticamente pelos controllers
            // Este middleware serve como fallback e log

            await _next(context);

            // Se chegou aqui com erro 400, provavelmente foi validação
            if (context.Response.StatusCode == 400)
            {
                _logger.LogWarning(
                    "⚠️ Validation failed | Path: {Path} | Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);
            }
        }
    }

}
