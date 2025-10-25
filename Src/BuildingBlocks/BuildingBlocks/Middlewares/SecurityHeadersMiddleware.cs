using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Middlewares
{
    /// <summary>
    /// Middleware para aplicação de cabeçalhos de segurança HTTP recomendados pela OWASP.
    /// 
    /// Este middleware implementa as melhores práticas de segurança web através da
    /// adição automática de cabeçalhos HTTP de segurança em todas as respostas,
    /// fornecendo proteção contra várias categorias de ataques web comuns.
    /// 
    /// Cabeçalhos de segurança implementados:
    /// 
    /// 1. X-Content-Type-Options: nosniff
    ///    - Previne ataques de MIME type sniffing
    ///    - Força o navegador a respeitar o Content-Type declarado
    /// 
    /// 2. X-Frame-Options: DENY
    ///    - Previne ataques de clickjacking
    ///    - Impede que a página seja carregada em frames/iframes
    /// 
    /// 3. X-XSS-Protection: 1; mode=block
    ///    - Ativa proteção XSS do navegador (legacy, mas ainda útil)
    ///    - Bloqueia a página quando XSS é detectado
    /// 
    /// 4. Referrer-Policy: strict-origin-when-cross-origin
    ///    - Controla informações de referrer enviadas
    ///    - Balanceia privacidade e funcionalidade
    /// 
    /// 5. Content-Security-Policy (CSP)
    ///    - Previne ataques XSS e injeção de código
    ///    - Define fontes confiáveis para recursos
    ///    - Configuração básica que deve ser ajustada por aplicação
    /// 
    /// 6. Permissions-Policy
    ///    - Controla acesso a APIs sensíveis do navegador
    ///    - Desabilita recursos não utilizados (geolocalização, câmera, microfone)
    /// 
    /// Remoção de cabeçalhos informativos:
    /// - Remove "Server" e "X-Powered-By" para reduzir fingerprinting
    /// 
    /// Benefícios de segurança:
    /// - Proteção contra clickjacking, XSS, MIME sniffing
    /// - Controle granular de recursos e permissões
    /// - Redução da superfície de ataque
    /// - Compliance com padrões de segurança modernos
    /// - Melhoria na pontuação de ferramentas de auditoria de segurança
    /// 
    /// Considerações importantes:
    /// - CSP deve ser ajustado conforme necessidades da aplicação
    /// - Teste thoroughly após implementação
    /// - Monitore logs de violação de CSP
    /// - Considere usar Report-Only mode inicialmente para CSP
    /// 
    /// Uso: Registre este middleware no início do pipeline para garantir
    /// que todos os responses tenham os cabeçalhos de segurança aplicados.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Construtor do middleware de cabeçalhos de segurança.
        /// </summary>
        /// <param name="next">Próximo middleware no pipeline</param>
        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Método principal de execução do middleware.
        /// 
        /// Este método adiciona todos os cabeçalhos de segurança necessários
        /// à resposta HTTP antes de passar o controle para o próximo middleware
        /// no pipeline. Os cabeçalhos são aplicados independentemente do
        /// resultado da requisição.
        /// 
        /// Ordem de execução:
        /// 1. Adiciona cabeçalhos de proteção contra ataques
        /// 2. Configura políticas de segurança
        /// 3. Remove cabeçalhos informativos desnecessários
        /// 4. Continua com o processamento da requisição
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição atual</param>
        /// <returns>Task representando a operação assíncrona</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Cabeçalho 1: X-Content-Type-Options
            // Previne ataques de MIME type sniffing onde o navegador
            // tenta "adivinhar" o tipo de conteúdo ignorando o Content-Type
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // Cabeçalho 2: X-Frame-Options (previne clickjacking)
            // DENY: Impede completamente o carregamento em frames
            // Alternativas: SAMEORIGIN (permite apenas do mesmo domínio)
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // Cabeçalho 3: X-XSS-Protection
            // Ativa o filtro XSS legado do navegador
            // "1; mode=block" bloqueia a página quando XSS é detectado
            // Nota: Considerado legacy, CSP é a abordagem moderna preferida
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Cabeçalho 4: Referrer-Policy
            // Controla quais informações de referrer são enviadas
            // "strict-origin-when-cross-origin": envia origem completa para same-origin,
            // apenas origem para cross-origin HTTPS, nada para HTTP
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Cabeçalho 5: Content-Security-Policy (CSP)
            // Política de segurança de conteúdo - CRÍTICO para prevenir XSS
            // Configuração básica que deve ser ajustada conforme necessário:
            // - default-src 'self': apenas recursos do mesmo domínio por padrão
            // - script-src 'self': apenas scripts do mesmo domínio
            // - style-src 'self' 'unsafe-inline': estilos do mesmo domínio + inline CSS
            // 
            // ⚠️ IMPORTANTE: Ajuste esta política conforme sua aplicação!
            // Para SPAs, APIs externas, CDNs, etc., adicione domínios específicos
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

            // Cabeçalho 6: Permissions-Policy (anteriormente Feature-Policy)
            // Controla acesso a APIs sensíveis do navegador
            // Desabilita recursos não utilizados para reduzir superfície de ataque:
            // - geolocation=(): desabilita API de geolocalização
            // - microphone=(): desabilita acesso ao microfone
            // - camera=(): desabilita acesso à câmera
            // 
            // Adicione outras políticas conforme necessário:
            // payment=(), usb=(), magnetometer=(), gyroscope=()
            context.Response.Headers.Append("Permissions-Policy",
                "geolocation=(), microphone=(), camera=()");

            // Remoção de cabeçalhos informativos
            // Remove headers que expõem informações sobre o servidor
            // para reduzir fingerprinting e reconnaissance de atacantes
            context.Response.Headers.Remove("Server");        // Remove info do servidor web
            context.Response.Headers.Remove("X-Powered-By");  // Remove info do framework

            // Continua com o próximo middleware no pipeline
            // Os cabeçalhos já foram definidos e serão enviados com a resposta
            await _next(context);
        }
    }
}
