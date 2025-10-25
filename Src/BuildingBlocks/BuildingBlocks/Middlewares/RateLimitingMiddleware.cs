using BuildingBlocks.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildingBlocks.Middlewares
{
    //📌 NOTA: Em produção, use bibliotecas maduras como:
    //AspNetCoreRateLimit
    //Microsoft.AspNetCore.RateLimiting(.NET 7+)


    /// <summary>
    /// Implementação simples de rate limiting (usar biblioteca em produção)
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, RequestCounter> _requests = new();

        private const int MaxRequests = 100;
        private const int TimeWindowSeconds = 60;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);

            if (!IsAllowed(clientId))
            {
                _logger.LogWarning(
                    "⚠️ Rate limit exceeded for client: {ClientId} | " +
                    "Path: {Path}",
                    clientId,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Append("Retry-After", "60");
                context.Response.ContentType = "application/json";

                var response = ApiResponse.Fail("RATE_LIMIT_EXCEEDED",
                    "Muitas requisições. Tente novamente em alguns instantes.");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Priorizar IP real se atrás de proxy
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            // Em produção, considerar também user-agent ou token
            return ip;
        }

        private bool IsAllowed(string clientId)
        {
            var counter = _requests.GetOrAdd(clientId, _ => new RequestCounter());

            lock (counter)
            {
                var now = DateTime.UtcNow;

                // Limpar requisições antigas
                counter.Requests.RemoveAll(r =>
                    (now - r).TotalSeconds > TimeWindowSeconds);

                if (counter.Requests.Count >= MaxRequests)
                {
                    return false;
                }

                counter.Requests.Add(now);
                return true;
            }
        }

        private class RequestCounter
        {
            public List<DateTime> Requests { get; } = new();
        }
    }
}
