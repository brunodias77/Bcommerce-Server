using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.MessageBus.Examples;

/// <summary>
/// Exemplos de uso da camada de mensageria
/// </summary>
public static class MessageBusUsageExample
{
    /// <summary>
    /// Exemplo de configuração do MessageBus no DI container
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Opção 1: Configuração via appsettings.json
        services.AddMessageBus(configuration, typeof(MessageBusUsageExample).Assembly);
        
        // Opção 2: Configuração manual
        services.AddMessageBus(config =>
        {
            config.Host = "localhost";
            config.Port = 5672;
            config.Username = "admin";
            config.Password = "password";
            config.ServiceName = "BCommerce.API";
        }, typeof(MessageBusUsageExample).Assembly);
        
        // Opção 3: Para desenvolvimento
        services.AddMessageBusForDevelopment(typeof(MessageBusUsageExample).Assembly);
    }
}

/// <summary>
/// Exemplo de evento de domínio
/// </summary>
public class ProductCreatedEvent : MessageBase, IEvent
{
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; }
    public string Source { get; set; } = "ProductService";
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    
    public ProductCreatedEvent()
    {
        MessageType = nameof(ProductCreatedEvent);
    }
}

/// <summary>
/// Exemplo de comando
/// </summary>
public class CreateProductCommand : MessageBase, ICommand
{
    public Guid UserId { get; set; }
    public Guid CorrelationId { get; set; }
    public string TargetService { get; set; } = "ProductService";
    public int Priority { get; set; } = 1;
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    
    public CreateProductCommand()
    {
        MessageType = nameof(CreateProductCommand);
        CorrelationId = Guid.NewGuid();
    }
}

/// <summary>
/// Exemplo de handler para evento
/// </summary>
public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;
    
    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Produto criado: {ProductName} - Preço: {Price}", 
            @event.ProductName, @event.Price);
        
        // Lógica de processamento do evento
        // Ex: Atualizar cache, enviar notificação, etc.
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Exemplo de handler para comando
/// </summary>
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand>
{
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly IMessageBus _messageBus;
    
    public CreateProductCommandHandler(
        ILogger<CreateProductCommandHandler> logger,
        IMessageBus messageBus)
    {
        _logger = logger;
        _messageBus = messageBus;
    }
    
    public async Task HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processando comando para criar produto: {ProductName}", 
            command.ProductName);
        
        try
        {
            // Lógica de criação do produto
            // Ex: Validar dados, salvar no banco, etc.
            
            // Publicar evento de produto criado
            var productCreatedEvent = new ProductCreatedEvent
            {
                AggregateId = Guid.NewGuid(),
                AggregateVersion = 1,
                ProductName = command.ProductName,
                Price = command.Price,
                Category = command.Category
            };
            
            await _messageBus.PublishEventAsync(productCreatedEvent, cancellationToken);
            
            _logger.LogInformation("Produto criado com sucesso: {ProductName}", command.ProductName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comando de criação de produto");
            throw;
        }
    }
}

/// <summary>
/// Exemplo de uso do MessageBus em um serviço
/// </summary>
public class ProductService
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(IMessageBus messageBus, ILogger<ProductService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }
    
    public async Task CreateProductAsync(string name, decimal price, string category)
    {
        var command = new CreateProductCommand
        {
            UserId = Guid.NewGuid(), // Normalmente viria do contexto do usuário
            ProductName = name,
            Price = price,
            Category = category
        };
        
        await _messageBus.SendCommandAsync(command);
        _logger.LogInformation("Comando de criação de produto enviado: {ProductName}", name);
    }
    
    public async Task StartListeningAsync()
    {
        // Subscrever a comandos
        await _messageBus.SubscribeCommandAsync<CreateProductCommand>("product.commands");
        
        // Subscrever a eventos
        await _messageBus.SubscribeEventAsync<ProductCreatedEvent>("product.events");
        
        // Iniciar consumo
        await _messageBus.StartConsumingAsync();
        
        _logger.LogInformation("ProductService iniciado e ouvindo mensagens");
    }
    
    public async Task StopListeningAsync()
    {
        await _messageBus.StopConsumingAsync();
        _logger.LogInformation("ProductService parou de ouvir mensagens");
    }
}