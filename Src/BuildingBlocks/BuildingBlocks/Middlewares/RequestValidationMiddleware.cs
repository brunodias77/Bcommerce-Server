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
    /// Middleware para validação de requisições HTTP antes do processamento.
    /// 
    /// Este middleware implementa validações básicas de segurança e formato para
    /// requisições HTTP, garantindo que apenas requisições bem formadas e dentro
    /// dos limites estabelecidos sejam processadas pela aplicação.
    /// 
    /// Funcionalidades principais:
    /// - Validação de Content-Type para métodos que enviam dados (POST, PUT, PATCH)
    /// - Validação do tamanho máximo do body da requisição
    /// - Rejeição precoce de requisições inválidas com respostas padronizadas
    /// - Logging de tentativas de requisições inválidas para monitoramento
    /// - Proteção contra ataques de payload excessivamente grandes
    /// 
    /// Validações implementadas:
    /// 1. Content-Type: Deve ser "application/json" para métodos POST/PUT/PATCH
    /// 2. Tamanho do body: Máximo de 10MB por requisição
    /// 
    /// Benefícios de segurança:
    /// - Previne ataques de DoS através de payloads grandes
    /// - Garante consistência no formato de dados recebidos
    /// - Reduz carga de processamento ao rejeitar requisições inválidas cedo
    /// - Fornece feedback claro sobre problemas de formato
    /// 
    /// Uso: Registre este middleware no início do pipeline, após middlewares
    /// de autenticação mas antes de middlewares de processamento de dados.
    /// </summary>
    public class RequestValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestValidationMiddleware> _logger;
        
        /// <summary>
        /// Tamanho máximo permitido para o body de uma requisição em bytes.
        /// 
        /// Valor atual: 10MB (10 * 1024 * 1024 bytes)
        /// 
        /// Este limite ajuda a prevenir:
        /// - Ataques de negação de serviço (DoS) através de payloads grandes
        /// - Consumo excessivo de memória no servidor
        /// - Timeouts de requisição devido a uploads grandes
        /// 
        /// Considerações para produção:
        /// - Ajuste baseado nos requisitos da aplicação
        /// - Considere limites diferentes para endpoints específicos
        /// - Monitore o uso de memória e performance
        /// - Para uploads de arquivos, considere streaming ou serviços dedicados
        /// </summary>
        private const int MaxRequestBodySize = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Construtor do middleware de validação de requisições.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar eventos de validação</param>
        public RequestValidationMiddleware(
            RequestDelegate next,
            ILogger<RequestValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método:
        /// 1. Verifica se o método HTTP requer validação de conteúdo
        /// 2. Valida o Content-Type da requisição
        /// 3. Valida o tamanho do body da requisição
        /// 4. Rejeita requisições inválidas com respostas apropriadas
        /// 5. Permite que requisições válidas continuem no pipeline
        /// 
        /// As validações são aplicadas apenas para métodos que tipicamente
        /// enviam dados no body (POST, PUT, PATCH). Métodos como GET e DELETE
        /// não passam por essas validações.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Aplica validações apenas para métodos que enviam dados no body
            // GET, DELETE, HEAD, OPTIONS normalmente não têm body
            if (context.Request.Method is "POST" or "PUT" or "PATCH")
            {
                // Validação 1: Content-Type deve ser application/json
                var contentType = context.Request.ContentType;

                // Verifica se o Content-Type está presente e é válido
                if (string.IsNullOrEmpty(contentType) ||
                    !contentType.StartsWith("application/json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Registra tentativa de requisição com Content-Type inválido
                    _logger.LogWarning(
                        "⚠️ Requisição rejeitada: Content-Type inválido {ContentType}",
                        contentType);

                    // Configura resposta de erro 415 (Unsupported Media Type)
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    context.Response.ContentType = "application/json";
                    
                    // Cria resposta padronizada de erro
                    var response = ApiResponse.Fail("INVALID_CONTENT_TYPE",
                        "Content-Type deve ser application/json");
                    
                    // Serializa e envia a resposta de erro
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return; // Interrompe o pipeline
                }

                // Validação 2: Tamanho do body não deve exceder o limite
                if (context.Request.ContentLength > MaxRequestBodySize)
                {
                    // Registra tentativa de requisição com payload muito grande
                    // Converte bytes para MB para melhor legibilidade no log
                    _logger.LogWarning(
                        "⚠️ Requisição rejeitada: Body muito grande {Size}MB",
                        context.Request.ContentLength / 1024 / 1024);

                    // Configura resposta de erro 413 (Payload Too Large)
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    context.Response.ContentType = "application/json";
                    
                    // Cria resposta padronizada de erro
                    var response = ApiResponse.Fail("PAYLOAD_TOO_LARGE",
                        "Body da requisição excede o limite de 10MB");
                    
                    // Serializa e envia a resposta de erro
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return; // Interrompe o pipeline
                }
            }

            // Se todas as validações passaram, continua com o próximo middleware
            await _next(context);
        }
    }
}
