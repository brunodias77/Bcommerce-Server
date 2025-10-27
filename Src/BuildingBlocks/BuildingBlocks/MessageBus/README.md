# MessageBus - Camada de Mensageria

Esta é uma implementação robusta de uma camada de mensageria para o projeto BCommerce, utilizando RabbitMQ como broker de mensagens.

## Características

- ✅ **Abstrações bem definidas**: Interfaces para mensagens, eventos, comandos e handlers
- ✅ **Integração com RabbitMQ**: Implementação completa com conexão, reconexão automática e tratamento de erros
- ✅ **Serialização JSON**: Usando Newtonsoft.Json para serialização/deserialização
- ✅ **Logging integrado**: Usando Microsoft.Extensions.Logging
- ✅ **Dependency Injection**: Extensions para configuração automática no DI container
- ✅ **Padrões assíncronos**: Suporte completo a async/await com CancellationToken
- ✅ **Configuração flexível**: Múltiplas opções de configuração (appsettings, código, desenvolvimento)

## Estrutura dos Arquivos

```
MessageBus/
├── IMessage.cs                    # Interface base para todas as mensagens
├── IEvent.cs                      # Interface para eventos de domínio
├── ICommand.cs                    # Interface para comandos
├── MessageBase.cs                 # Classe base abstrata para mensagens
├── IMessageHandler.cs             # Interfaces para handlers de mensagens
├── IMessageBus.cs                 # Interface principal do message bus
├── MessageBus.cs                  # Implementação do message bus com RabbitMQ
├── MessageBusConfiguration.cs     # Classe de configuração
├── ServiceCollectionExtensions.cs # Extensions para DI
└── Examples/
    └── MessageBusUsageExample.cs  # Exemplos de uso
```

## Como Usar

### 1. Configuração no DI Container

```csharp
// No Program.cs ou Startup.cs
services.AddMessageBus(configuration, typeof(Program).Assembly);

// Ou configuração manual
services.AddMessageBus(config =>
{
    config.Host = "localhost";
    config.Port = 5672;
    config.Username = "admin";
    config.Password = "password";
    config.ServiceName = "BCommerce.API";
}, typeof(Program).Assembly);

// Para desenvolvimento
services.AddMessageBusForDevelopment(typeof(Program).Assembly);
```

### 2. Configuração no appsettings.json

```json
{
  "MessageBus": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "password",
    "VirtualHost": "/",
    "ServiceName": "BCommerce.API",
    "ExchangePrefix": "bcommerce",
    "QueuePrefix": "bcommerce",
    "ConnectionTimeoutSeconds": 30,
    "HeartbeatIntervalSeconds": 60,
    "MaxRetryAttempts": 5,
    "RetryIntervalSeconds": 5,
    "UseSsl": false,
    "AutoDeclareTopology": true
  }
}
```

### 3. Definindo Mensagens

```csharp
// Evento
public class ProductCreatedEvent : MessageBase, IEvent
{
    public Guid AggregateId { get; set; }
    public int AggregateVersion { get; set; }
    public string Source { get; set; } = "ProductService";
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Comando
public class CreateProductCommand : MessageBase, ICommand
{
    public Guid UserId { get; set; }
    public Guid CorrelationId { get; set; }
    public string TargetService { get; set; } = "ProductService";
    public int Priority { get; set; } = 1;
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### 4. Implementando Handlers

```csharp
public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;
    
    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Produto criado: {ProductName}", @event.ProductName);
        // Lógica de processamento
    }
}
```

### 5. Usando o MessageBus

```csharp
public class ProductService
{
    private readonly IMessageBus _messageBus;
    
    public ProductService(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }
    
    public async Task CreateProductAsync(string name, decimal price)
    {
        // Enviar comando
        var command = new CreateProductCommand
        {
            ProductName = name,
            Price = price
        };
        
        await _messageBus.SendCommandAsync(command);
        
        // Publicar evento
        var @event = new ProductCreatedEvent
        {
            AggregateId = Guid.NewGuid(),
            ProductName = name,
            Price = price
        };
        
        await _messageBus.PublishEventAsync(@event);
    }
    
    public async Task StartListeningAsync()
    {
        await _messageBus.SubscribeCommandAsync<CreateProductCommand>("product.commands");
        await _messageBus.SubscribeEventAsync<ProductCreatedEvent>("product.events");
        await _messageBus.StartConsumingAsync();
    }
}
```

## Funcionalidades Avançadas

### Declaração Manual de Topologia

```csharp
// Declarar exchange
await _messageBus.DeclareExchangeAsync("products", "topic", durable: true);

// Declarar fila
await _messageBus.DeclareQueueAsync("product.commands", durable: true, exclusive: false, autoDelete: false);

// Fazer binding
await _messageBus.BindQueueAsync("product.commands", "products", "product.command.*");
```

### Controle de Consumo

```csharp
// Iniciar consumo
await _messageBus.StartConsumingAsync();

// Parar consumo
await _messageBus.StopConsumingAsync();
```

## Padrões de Nomenclatura

- **Exchanges**: `{ExchangePrefix}.{domain}.{type}` (ex: `bcommerce.product.events`)
- **Filas**: `{QueuePrefix}.{service}.{domain}.{type}` (ex: `bcommerce.api.product.commands`)
- **Routing Keys**: `{domain}.{type}.{action}` (ex: `product.event.created`)

## Tratamento de Erros

A implementação inclui:
- Reconexão automática em caso de falha
- Retry com backoff exponencial
- Logging detalhado de erros
- Tratamento de exceções em handlers

## Integração com o Projeto

Esta camada de mensageria foi projetada para se integrar perfeitamente com:
- **Mediator**: Para comunicação interna
- **ValidationHandler**: Para tratamento de erros
- **Logging**: Para auditoria e debugging
- **Dependency Injection**: Para configuração automática

## Próximos Passos

Para usar esta implementação em produção, considere:
1. Configurar RabbitMQ com clustering para alta disponibilidade
2. Implementar dead letter queues para mensagens com falha
3. Adicionar métricas e monitoramento
4. Configurar SSL/TLS para comunicação segura
5. Implementar circuit breaker para resiliência