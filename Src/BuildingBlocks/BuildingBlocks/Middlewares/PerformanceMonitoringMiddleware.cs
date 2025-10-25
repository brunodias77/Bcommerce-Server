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
    /// Monitora performance e alerta sobre requisições lentas
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private const int SlowRequestThresholdMs = 1000; // 1 segundo

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "🐌 SLOW REQUEST | Path: {Path} | Method: {Method} | " +
                        "Duration: {Duration}ms | StatusCode: {StatusCode}",
                        context.Request.Path,
                        context.Request.Method,
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
        }
    }
}
