using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Middlewares
{
    /// <summary>
    /// Adiciona headers de segurança recomendados (OWASP)
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // X-Content-Type-Options
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // X-Frame-Options (previne clickjacking)
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // X-XSS-Protection
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Referrer-Policy
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content-Security-Policy (ajustar conforme necessário)
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

            // Permissions-Policy (anteriormente Feature-Policy)
            context.Response.Headers.Append("Permissions-Policy",
                "geolocation=(), microphone=(), camera=()");

            // Remove header que expõe versão do servidor
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }
}
