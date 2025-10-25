using BuildingBlocks.Results;
using BuildingBlocks.Validations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace BuildingBlocks.Middlewares
{
    /// <summary>
    /// Middleware global para tratamento consistente de exceções
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception,
                "💥 Exceção não tratada: {Message} | Path: {Path} | Method: {Method}",
                exception.Message,
                context.Request.Path,
                context.Request.Method);

            (int statusCode, ApiResponse response) = exception switch
            {
                ValidationException validationEx => (
                    StatusCodes.Status400BadRequest,
                    ApiResponse.Fail(new ValidationHandler()
                        .Merge(new ValidationHandler { }))),

                UnauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    ApiResponse.Fail("UNAUTHORIZED", "Não autorizado")),

                KeyNotFoundException => (
                    StatusCodes.Status404NotFound,
                    ApiResponse.Fail("NOT_FOUND", "Recurso não encontrado")),

                OperationCanceledException => (
                    StatusCodes.Status408RequestTimeout,
                    ApiResponse.Fail("REQUEST_CANCELLED", "Requisição cancelada")),

                InvalidOperationException invalidEx => (
                    StatusCodes.Status400BadRequest,
                    ApiResponse.Fail("INVALID_OPERATION", invalidEx.Message)),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("INTERNAL_ERROR",
                        "Erro interno do servidor. Tente novamente mais tarde."))
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}
