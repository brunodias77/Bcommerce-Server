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
    /// <summary>
    /// ⚠️ AVISO IMPORTANTE PARA PRODUÇÃO ⚠️
    /// 
    /// Esta é uma implementação SIMPLES de rate limiting para fins educacionais e desenvolvimento.
    /// 
    /// Para ambientes de PRODUÇÃO, utilize bibliotecas maduras e testadas como:
    /// - AspNetCoreRateLimit (https://github.com/stefanprodan/AspNetCoreRateLimit)
    /// - Microsoft.AspNetCore.RateLimiting (.NET 7+)
    /// - Redis-based solutions para aplicações distribuídas
    /// 
    /// Limitações desta implementação:
    /// - Armazena dados em memória (perdidos ao reiniciar)
    /// - Não funciona em cenários multi-instância/load balancer
    /// - Não possui persistência ou recuperação de estado
    /// - Algoritmo simples de janela deslizante
    /// </summary>

    /// <summary>
    /// Middleware de rate limiting (limitação de taxa de requisições) simples.
    /// 
    /// Este middleware implementa um sistema básico de controle de taxa que limita
    /// o número de requisições que um cliente pode fazer dentro de uma janela de tempo.
    /// 
    /// Funcionalidades principais:
    /// - Controla o número máximo de requisições por cliente
    /// - Utiliza janela de tempo deslizante para contagem
    /// - Identifica clientes por endereço IP (com suporte a proxies)
    /// - Retorna erro 429 (Too Many Requests) quando limite é excedido
    /// - Adiciona header "Retry-After" para indicar quando tentar novamente
    /// - Registra logs de violações de rate limit
    /// 
    /// Configuração atual:
    /// - Máximo: 100 requisições por cliente
    /// - Janela de tempo: 60 segundos
    /// 
    /// Algoritmo: Janela deslizante que remove requisições antigas automaticamente
    /// e permite novas requisições conforme o tempo passa.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        
        /// <summary>
        /// Dicionário thread-safe para armazenar contadores de requisições por cliente.
        /// Utiliza ConcurrentDictionary para suportar acesso concorrente seguro.
        /// 
        /// Chave: Identificador do cliente (normalmente IP)
        /// Valor: Objeto RequestCounter com histórico de requisições
        /// </summary>
        private static readonly ConcurrentDictionary<string, RequestCounter> _requests = new();

        /// <summary>
        /// Número máximo de requisições permitidas por cliente na janela de tempo.
        /// 
        /// Valor atual: 100 requisições
        /// 
        /// Nota: Em produção, este valor deve ser configurável através de
        /// appsettings.json ou variáveis de ambiente.
        /// </summary>
        private const int MaxRequests = 100;
        
        /// <summary>
        /// Tamanho da janela de tempo em segundos para contagem de requisições.
        /// 
        /// Valor atual: 60 segundos (1 minuto)
        /// 
        /// Nota: Em produção, este valor deve ser configurável e pode variar
        /// por endpoint ou tipo de cliente.
        /// </summary>
        private const int TimeWindowSeconds = 60;

        /// <summary>
        /// Construtor do middleware de rate limiting.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar eventos de rate limiting</param>
        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método:
        /// 1. Identifica o cliente através do IP
        /// 2. Verifica se o cliente excedeu o limite de requisições
        /// 3. Se excedeu: retorna erro 429 com informações de retry
        /// 4. Se não excedeu: permite que a requisição continue
        /// 
        /// O controle é feito por cliente individual, permitindo que diferentes
        /// clientes tenham seus próprios contadores independentes.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Obtém o identificador único do cliente (normalmente o IP)
            var clientId = GetClientIdentifier(context);

            // Verifica se o cliente excedeu o limite de requisições
            if (!IsAllowed(clientId))
            {
                // Registra a violação do rate limit para monitoramento
                _logger.LogWarning(
                    "⚠️ Rate limit exceeded for client: {ClientId} | " +
                    "Path: {Path}",
                    clientId,
                    context.Request.Path);

                // Configura a resposta de erro 429 (Too Many Requests)
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                // Adiciona header indicando quando o cliente pode tentar novamente
                context.Response.Headers.Append("Retry-After", "60");
                
                // Define o tipo de conteúdo como JSON
                context.Response.ContentType = "application/json";

                // Cria uma resposta padronizada de erro
                var response = ApiResponse.Fail("RATE_LIMIT_EXCEEDED",
                    "Muitas requisições. Tente novamente em alguns instantes.");
                
                // Serializa e envia a resposta de erro
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return; // Interrompe o pipeline, não executa próximos middlewares
            }

            // Se o rate limit não foi excedido, continua com o próximo middleware
            await _next(context);
        }

        /// <summary>
        /// Obtém um identificador único para o cliente baseado no endereço IP.
        /// 
        /// Este método:
        /// 1. Verifica primeiro o header X-Forwarded-For (para casos com proxy/load balancer)
        /// 2. Se não encontrar, usa o IP da conexão direta
        /// 3. Como fallback, usa "unknown"
        /// 
        /// Considerações para produção:
        /// - Pode incluir User-Agent para identificação mais precisa
        /// - Pode usar tokens de autenticação quando disponíveis
        /// - Deve tratar adequadamente cenários com múltiplos proxies
        /// - Considerar validação e sanitização do IP
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição</param>
        /// <returns>String identificadora única do cliente</returns>
        private string GetClientIdentifier(HttpContext context)
        {
            // Prioriza o IP real quando a aplicação está atrás de proxy/load balancer
            // X-Forwarded-For é o header padrão usado por proxies para indicar o IP original
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";

            // Em produção, considere também incluir:
            // - User-Agent para diferenciação adicional
            // - Token de autenticação quando disponível
            // - Outros headers identificadores
            return ip;
        }

        /// <summary>
        /// Verifica se um cliente está dentro do limite de requisições permitidas.
        /// 
        /// Este método implementa o algoritmo de janela deslizante:
        /// 1. Obtém ou cria um contador para o cliente
        /// 2. Remove requisições antigas (fora da janela de tempo)
        /// 3. Verifica se o número atual de requisições excede o limite
        /// 4. Se não excede, adiciona a requisição atual ao contador
        /// 
        /// O uso de lock garante thread-safety quando múltiplas requisições
        /// do mesmo cliente chegam simultaneamente.
        /// </summary>
        /// <param name="clientId">Identificador único do cliente</param>
        /// <returns>True se a requisição é permitida, False se excede o limite</returns>
        private bool IsAllowed(string clientId)
        {
            // Obtém ou cria um novo contador para o cliente
            // GetOrAdd é thread-safe e garante que apenas um contador seja criado por cliente
            var counter = _requests.GetOrAdd(clientId, _ => new RequestCounter());

            // Lock garante que apenas uma thread modifique o contador por vez
            // Isso é crucial para evitar condições de corrida
            lock (counter)
            {
                var now = DateTime.UtcNow;

                // Remove requisições antigas que estão fora da janela de tempo
                // Implementa a lógica de "janela deslizante"
                counter.Requests.RemoveAll(r =>
                    (now - r).TotalSeconds > TimeWindowSeconds);

                // Verifica se o cliente já atingiu o limite máximo de requisições
                if (counter.Requests.Count >= MaxRequests)
                {
                    return false; // Rate limit excedido
                }

                // Adiciona a requisição atual ao contador
                counter.Requests.Add(now);
                return true; // Requisição permitida
            }
        }

        /// <summary>
        /// Classe interna para armazenar o histórico de requisições de um cliente.
        /// 
        /// Mantém uma lista de timestamps das requisições realizadas,
        /// permitindo implementar o algoritmo de janela deslizante.
        /// 
        /// Nota: Em produção, considere:
        /// - Limitar o tamanho máximo da lista para evitar uso excessivo de memória
        /// - Implementar limpeza periódica de clientes inativos
        /// - Usar estruturas de dados mais eficientes para grandes volumes
        /// </summary>
        private class RequestCounter
        {
            /// <summary>
            /// Lista de timestamps das requisições realizadas pelo cliente.
            /// Cada DateTime representa o momento de uma requisição.
            /// </summary>
            public List<DateTime> Requests { get; } = new();
        }
    }
}
