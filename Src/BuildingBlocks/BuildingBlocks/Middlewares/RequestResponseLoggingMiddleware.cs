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
    /// Middleware para logging detalhado de requisições e respostas HTTP.
    /// 
    /// Este middleware captura e registra informações completas sobre todas as requisições
    /// e respostas que passam pela aplicação, fornecendo rastreabilidade e debugging avançado.
    /// 
    /// Funcionalidades principais:
    /// - Gera um ID de correlação único para cada requisição
    /// - Registra detalhes completos da requisição (método, path, headers, body)
    /// - Registra detalhes completos da resposta (status, headers, body, tempo de processamento)
    /// - Mede o tempo total de processamento de cada requisição
    /// - Adiciona header X-Correlation-Id para rastreamento distribuído
    /// - Utiliza diferentes níveis de log baseados no status code da resposta
    /// - Suporta buffering para capturar bodies de requisição e resposta
    /// 
    /// Benefícios:
    /// - Rastreabilidade completa de requisições em sistemas distribuídos
    /// - Debugging facilitado através de logs estruturados
    /// - Monitoramento de performance por requisição
    /// - Auditoria completa de tráfego HTTP
    /// - Correlação entre logs de diferentes serviços
    /// 
    /// Considerações de performance:
    /// - Pode impactar performance em aplicações de alto volume
    /// - Bodies são carregados em memória para logging
    /// - Considere filtrar endpoints sensíveis ou de alta frequência
    /// 
    /// Uso: Registre este middleware no início do pipeline para capturar
    /// todas as requisições, ou após middlewares de autenticação se necessário.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        /// <summary>
        /// Construtor do middleware de logging de requisições e respostas.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar informações de requisições e respostas</param>
        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método:
        /// 1. Gera um ID de correlação único para a requisição
        /// 2. Adiciona o ID de correlação aos headers da resposta
        /// 3. Registra informações detalhadas da requisição
        /// 4. Configura captura do body da resposta
        /// 5. Mede o tempo de processamento
        /// 6. Executa o próximo middleware
        /// 7. Registra informações detalhadas da resposta
        /// 8. Restaura o stream original da resposta
        /// 
        /// O uso de try/finally garante que o logging da resposta sempre ocorra,
        /// mesmo em caso de exceções durante o processamento.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Gera um ID único para correlacionar logs desta requisição
            // Útil para rastrear requisições em sistemas distribuídos
            var correlationId = Guid.NewGuid().ToString();
            
            // Adiciona o ID de correlação aos headers da resposta
            // Permite que clientes e outros serviços correlacionem logs
            context.Response.Headers.Append("X-Correlation-Id", correlationId);

            // Registra informações detalhadas da requisição recebida
            await LogRequestAsync(context, correlationId);

            // Configura captura do body da resposta
            // Necessário porque o Response.Body é um stream write-only por padrão
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Inicia cronômetro para medir tempo de processamento
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Executa o próximo middleware no pipeline
                await _next(context);
            }
            finally
            {
                // Para o cronômetro independentemente de sucesso ou falha
                stopwatch.Stop();

                // Registra informações detalhadas da resposta gerada
                await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

                // Restaura o conteúdo da resposta para o stream original
                // Necessário para que a resposta seja enviada ao cliente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        /// <summary>
        /// Registra informações detalhadas da requisição HTTP recebida.
        /// 
        /// Este método:
        /// 1. Habilita buffering para permitir múltiplas leituras do body
        /// 2. Lê o conteúdo do body da requisição (se presente)
        /// 3. Registra informações estruturadas da requisição
        /// 4. Reposiciona o stream do body para o início
        /// 
        /// O buffering é necessário porque o Request.Body é um stream forward-only,
        /// e precisamos lê-lo sem impedir que outros middlewares o utilizem.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição</param>
        /// <param name="correlationId">ID de correlação único da requisição</param>
        /// <returns>Task representando a operação assíncrona</returns>
        private async Task LogRequestAsync(HttpContext context, string correlationId)
        {
            // Habilita buffering para permitir múltiplas leituras do Request.Body
            // Sem isso, o stream só pode ser lido uma vez
            context.Request.EnableBuffering();

            var request = context.Request;
            var body = string.Empty;

            // Lê o body da requisição se houver conteúdo
            if (request.ContentLength > 0)
            {
                // Reposiciona para o início do stream
                request.Body.Seek(0, SeekOrigin.Begin);
                
                // Lê o conteúdo completo do body
                // leaveOpen: true mantém o stream aberto para outros middlewares
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                
                // Reposiciona novamente para o início para próximos middlewares
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Registra informações estruturadas da requisição
            // O emoji 📥 facilita identificação visual de logs de entrada
            _logger.LogInformation(
                "📥 HTTP Request | Correlation: {CorrelationId} | " +
                "Method: {Method} | Path: {Path} | QueryString: {QueryString} | " +
                "ContentType: {ContentType} | ContentLength: {ContentLength}",
                correlationId,
                request.Method,          // GET, POST, PUT, DELETE, etc.
                request.Path,            // Caminho da URL (/api/users)
                request.QueryString,     // Parâmetros de query (?id=123&name=test)
                request.ContentType,     // Tipo de conteúdo (application/json)
                request.ContentLength);  // Tamanho do body em bytes

            // Registra o body apenas em nível Debug para evitar logs excessivos
            // Em produção, considere filtrar informações sensíveis
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogDebug("📄 Request Body: {Body}", body);
            }
        }

        /// <summary>
        /// Registra informações detalhadas da resposta HTTP gerada.
        /// 
        /// Este método:
        /// 1. Lê o conteúdo do body da resposta
        /// 2. Determina o nível de log baseado no status code
        /// 3. Registra informações estruturadas da resposta
        /// 4. Reposiciona o stream da resposta para o início
        /// 
        /// O nível de log é determinado pelo status code:
        /// - 5xx: Error (problemas do servidor)
        /// - 4xx: Warning (problemas do cliente)
        /// - Outros: Information (sucesso)
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição</param>
        /// <param name="correlationId">ID de correlação único da requisição</param>
        /// <param name="elapsedMs">Tempo decorrido em milissegundos</param>
        /// <returns>Task representando a operação assíncrona</returns>
        private async Task LogResponseAsync(
            HttpContext context,
            string correlationId,
            long elapsedMs)
        {
            var response = context.Response;
            
            // Lê o conteúdo do body da resposta
            response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            // Determina o nível de log baseado no status code da resposta
            // Isso permite filtrar logs por severidade
            var logLevel = response.StatusCode >= 500
                ? LogLevel.Error      // 5xx: Erros do servidor
                : response.StatusCode >= 400
                    ? LogLevel.Warning    // 4xx: Erros do cliente
                    : LogLevel.Information; // 2xx, 3xx: Sucesso

            // Registra informações estruturadas da resposta
            // O emoji 📤 facilita identificação visual de logs de saída
            _logger.Log(logLevel,
                "📤 HTTP Response | Correlation: {CorrelationId} | " +
                "StatusCode: {StatusCode} | ContentType: {ContentType} | " +
                "Duration: {Duration}ms",
                correlationId,
                response.StatusCode,     // Código de status HTTP (200, 404, 500, etc.)
                response.ContentType,    // Tipo de conteúdo da resposta
                elapsedMs);             // Tempo total de processamento

            // Registra o body da resposta apenas em nível Debug
            // Em produção, considere filtrar informações sensíveis
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogDebug("📄 Response Body: {Body}", body);
            }
        }
    }
}
