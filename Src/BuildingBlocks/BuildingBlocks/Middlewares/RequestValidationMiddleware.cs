using BuildingBlocks.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildingBlocks.Middlewares
{
    /// <summary>
    /// Middleware para validação de requisições (Content-Type, tamanho, etc)
    /// </summary>
    public class RequestValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestValidationMiddleware> _logger;
        private const int MaxRequestBodySize = 10 * 1024 * 1024; // 10MB

        public RequestValidationMiddleware(
            RequestDelegate next,
            ILogger<RequestValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Validar Content-Type para POST/PUT
            if (context.Request.Method is "POST" or "PUT" or "PATCH")
            {
                var contentType = context.Request.ContentType;

                if (string.IsNullOrEmpty(contentType) ||
                    !contentType.StartsWith("application/json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "⚠️ Requisição rejeitada: Content-Type inválido {ContentType}",
                        contentType);

                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    context.Response.ContentType = "application/json";
                    var response = ApiResponse.Fail("INVALID_CONTENT_TYPE",
                        "Content-Type deve ser application/json");
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Validar tamanho do body
                if (context.Request.ContentLength > MaxRequestBodySize)
                {
                    _logger.LogWarning(
                        "⚠️ Requisição rejeitada: Body muito grande {Size}MB",
                        context.Request.ContentLength / 1024 / 1024);

                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    context.Response.ContentType = "application/json";
                    var response = ApiResponse.Fail("PAYLOAD_TOO_LARGE",
                        "Body da requisição excede o limite de 10MB");
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }

            await _next(context);
        }
    }
}
