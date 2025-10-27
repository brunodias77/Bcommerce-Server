namespace BuildingBlocks.MessageBus;

public class MessageBusOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    /// <summary>
    /// Nome do serviço (ex: "catalog-service", "order-service")
    /// Usado para criar filas únicas por serviço
    /// </summary>
    public string ServiceName { get; set; } = "unknown";
}