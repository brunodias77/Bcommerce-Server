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
    /// Middleware para log detalhado de requisições e respostas
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = Guid.NewGuid().ToString();
            context.Response.Headers.Append("X-Correlation-Id", correlationId);

            // Log da requisição
            await LogRequestAsync(context, correlationId);

            // Capturar response body
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Log da resposta
                await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpContext context, string correlationId)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var body = string.Empty;

            if (request.ContentLength > 0)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            _logger.LogInformation(
                "📥 HTTP Request | Correlation: {CorrelationId} | " +
                "Method: {Method} | Path: {Path} | QueryString: {QueryString} | " +
                "ContentType: {ContentType} | ContentLength: {ContentLength}",
                correlationId,
                request.Method,
                request.Path,
                request.QueryString,
                request.ContentType,
                request.ContentLength);

            // Log do body apenas em ambiente de desenvolvimento
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogDebug("📄 Request Body: {Body}", body);
            }
        }

        private async Task LogResponseAsync(
            HttpContext context,
            string correlationId,
            long elapsedMs)
        {
            var response = context.Response;
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            var logLevel = response.StatusCode >= 500
                ? LogLevel.Error
                : response.StatusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;

            _logger.Log(logLevel,
                "📤 HTTP Response | Correlation: {CorrelationId} | " +
                "StatusCode: {StatusCode} | ContentType: {ContentType} | " +
                "Duration: {Duration}ms",
                correlationId,
                response.StatusCode,
                response.ContentType,
                elapsedMs);

            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogDebug("📄 Response Body: {Body}", body);
            }
        }
    }
}
