using BuildingBlocks.Results;
using BuildingBlocks.Validations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
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
    /// Middleware global para tratamento consistente de exceções em toda a aplicação.
    /// 
    /// Este middleware captura todas as exceções não tratadas que ocorrem durante o processamento
    /// de requisições HTTP e as converte em respostas JSON padronizadas com códigos de status apropriados.
    /// 
    /// Funcionalidades principais:
    /// - Captura exceções não tratadas globalmente
    /// - Converte exceções em respostas JSON consistentes
    /// - Mapeia tipos de exceção para códigos HTTP apropriados
    /// - Registra logs detalhados de todas as exceções
    /// - Evita vazamento de informações sensíveis em produção
    /// 
    /// Uso: Registre este middleware no início do pipeline de middlewares para garantir
    /// que todas as exceções sejam capturadas e tratadas adequadamente.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Construtor do middleware de tratamento de exceções.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        /// <param name="logger">Logger para registrar exceções e eventos</param>
        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// Invoca o próximo middleware no pipeline e captura qualquer exceção que possa ocorrer.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Executa o próximo middleware no pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Captura qualquer exceção não tratada e processa adequadamente
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Processa exceções capturadas, convertendo-as em respostas HTTP apropriadas.
        /// 
        /// Este método:
        /// 1. Registra a exceção no log com informações detalhadas
        /// 2. Mapeia o tipo de exceção para um código de status HTTP apropriado
        /// 3. Cria uma resposta JSON padronizada usando ApiResponse
        /// 4. Configura os headers da resposta HTTP
        /// 5. Serializa e envia a resposta ao cliente (se possível)
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição</param>
        /// <param name="exception">Exceção capturada para processamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Registra a exceção com informações contextuais importantes
            // O emoji 💥 facilita a identificação visual nos logs
            _logger.LogError(exception,
                "💥 Exceção não tratada: {Message} | Path: {Path} | Method: {Method}",
                exception.Message,
                context.Request.Path,
                context.Request.Method);

            // Verifica se a resposta já foi iniciada ou se o contexto foi disposed
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("⚠️ Não é possível modificar a resposta HTTP - resposta já foi iniciada");
                return;
            }

            try
            {
                // Mapeia diferentes tipos de exceção para códigos HTTP e respostas apropriadas
                // Utiliza pattern matching para determinar o tratamento adequado
                (int statusCode, ApiResponse response) = exception switch
                {
                    // Exceções de validação -> 400 Bad Request
                    ValidationException validationEx => (
                        StatusCodes.Status400BadRequest,
                        ApiResponse.Fail(new ValidationHandler()
                            .Merge(new ValidationHandler { }))),

                    // Acesso não autorizado -> 401 Unauthorized
                    UnauthorizedAccessException => (
                        StatusCodes.Status401Unauthorized,
                        ApiResponse.Fail("UNAUTHORIZED", "Não autorizado")),

                    // Recurso não encontrado -> 404 Not Found
                    KeyNotFoundException => (
                        StatusCodes.Status404NotFound,
                        ApiResponse.Fail("NOT_FOUND", "Recurso não encontrado")),

                    // Operação cancelada (timeout, cancellation token) -> 408 Request Timeout
                    OperationCanceledException => (
                        StatusCodes.Status408RequestTimeout,
                        ApiResponse.Fail("REQUEST_CANCELLED", "Requisição cancelada")),

                    // Exceções de conexão com PostgreSQL -> 500 Internal Server Error
                    NpgsqlException npgsqlEx => (
                        StatusCodes.Status500InternalServerError,
                        ApiResponse.Fail("DATABASE_ERROR", 
                            "Erro interno do servidor. Tente novamente mais tarde.")),

                    // Operação inválida -> 400 Bad Request
                    InvalidOperationException invalidEx => (
                        StatusCodes.Status400BadRequest,
                        ApiResponse.Fail("INVALID_OPERATION", invalidEx.Message)),

                    // Qualquer outra exceção -> 500 Internal Server Error
                    // Mensagem genérica para evitar vazamento de informações sensíveis
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        ApiResponse.Fail("INTERNAL_ERROR",
                            "Erro interno do servidor. Tente novamente mais tarde."))
                };

                // Configura o código de status HTTP da resposta
                context.Response.StatusCode = statusCode;
                
                // Define o tipo de conteúdo como JSON
                context.Response.ContentType = "application/json";

                // Configura opções de serialização JSON para consistência
                var jsonOptions = new JsonSerializerOptions
                {
                    // Usa camelCase para propriedades (padrão JavaScript/TypeScript)
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // Não indenta o JSON para reduzir o tamanho da resposta
                    WriteIndented = false
                };

                // Serializa a resposta para JSON
                var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);

                // Verifica novamente se ainda é possível escrever na resposta
                if (!context.Response.HasStarted && context.Response.Body.CanWrite)
                {
                    await context.Response.WriteAsync(jsonResponse);
                }
                else
                {
                    _logger.LogWarning("⚠️ Não foi possível escrever a resposta de erro - stream não disponível");
                }
            }
            catch (ObjectDisposedException)
            {
                // Se o contexto HTTP foi disposed, apenas registra no log
                _logger.LogWarning("⚠️ Contexto HTTP foi disposed - não é possível enviar resposta de erro");
            }
            catch (InvalidOperationException ioEx)
            {
                // Se houve problema ao modificar a resposta HTTP
                _logger.LogWarning(ioEx, "⚠️ Erro ao tentar modificar resposta HTTP: {Message}", ioEx.Message);
            }
            catch (Exception ex)
            {
                // Se houve qualquer outro erro ao processar a exceção original
                _logger.LogError(ex, "💥 Erro crítico ao processar exceção no middleware: {Message}", ex.Message);
            }
        }
    }
}