namespace BuildingBlocks.MessageBus;

/// <summary>
/// Configurações para conexão com RabbitMQ
/// </summary>
public class MessageBusConfiguration
{
    /// <summary>
    /// Host do RabbitMQ (padrão: localhost)
    /// </summary>
    public string Host { get; set; } = "localhost";
    
    /// <summary>
    /// Porta do RabbitMQ (padrão: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;
    
    /// <summary>
    /// Nome de usuário para autenticação
    /// </summary>
    public string Username { get; set; } = "guest";
    
    /// <summary>
    /// Senha para autenticação
    /// </summary>
    public string Password { get; set; } = "guest";
    
    /// <summary>
    /// Virtual host (padrão: /)
    /// </summary>
    public string VirtualHost { get; set; } = "/";
    
    /// <summary>
    /// Timeout para conexão em segundos (padrão: 30)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Intervalo de heartbeat em segundos (padrão: 60)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    
    /// <summary>
    /// Número máximo de tentativas de reconexão (padrão: 5)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;
    
    /// <summary>
    /// Intervalo entre tentativas de reconexão em segundos (padrão: 5)
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Prefixo para nomes de exchanges (padrão: bcommerce)
    /// </summary>
    public string ExchangePrefix { get; set; } = "bcommerce";
    
    /// <summary>
    /// Prefixo para nomes de filas (padrão: bcommerce)
    /// </summary>
    public string QueuePrefix { get; set; } = "bcommerce";
    
    /// <summary>
    /// Se deve usar SSL/TLS (padrão: false)
    /// </summary>
    public bool UseSsl { get; set; } = false;
    
    /// <summary>
    /// Nome do serviço atual (usado para routing)
    /// </summary>
    public string ServiceName { get; set; } = "unknown";
    
    /// <summary>
    /// Se deve declarar exchanges e filas automaticamente (padrão: true)
    /// </summary>
    public bool AutoDeclareTopology { get; set; } = true;
}