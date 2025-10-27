using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using BuildingBlocks.Validations;

namespace BuildingBlocks.MessageBus;

/// <summary>
/// Implementação do Message Bus usando RabbitMQ
/// </summary>
public class MessageBus : IMessageBus
{
    private readonly MessageBusConfiguration _configuration;
    private readonly ILogger<MessageBus> _logger;
    private readonly ConcurrentDictionary<string, Func<string, CancellationToken, Task>> _eventHandlers;
    private readonly ConcurrentDictionary<string, Func<string, CancellationToken, Task>> _commandHandlers;
    
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;
    private readonly object _lock = new();

    public MessageBus(IOptions<MessageBusConfiguration> configuration, ILogger<MessageBus> logger)
    {
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlers = new ConcurrentDictionary<string, Func<string, CancellationToken, Task>>();
        _commandHandlers = new ConcurrentDictionary<string, Func<string, CancellationToken, Task>>();
    }

    public async Task PublishEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class, IEvent
    {
        await EnsureConnectedAsync();
        
        var routingKey = $"event.{typeof(T).Name.ToLowerInvariant()}";
        var exchangeName = $"{_configuration.ExchangePrefix}.events";
        
        await DeclareExchangeAsync(exchangeName);
        
        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);
        
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = @event.MessageId.ToString();
        properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)@event.CreatedAt).ToUnixTimeSeconds());
        properties.Type = @event.MessageType;
        properties.Headers = new Dictionary<string, object>
        {
            ["source"] = @event.Source,
            ["version"] = @event.Version,
            ["aggregate_id"] = @event.AggregateId.ToString(),
            ["aggregate_version"] = @event.AggregateVersion
        };

        _channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Evento {EventType} publicado com ID {MessageId}", typeof(T).Name, @event.MessageId);
    }

    public async Task SendCommandAsync<T>(T command, CancellationToken cancellationToken = default) where T : class, ICommand
    {
        await EnsureConnectedAsync();
        
        var routingKey = $"command.{command.TargetService.ToLowerInvariant()}.{typeof(T).Name.ToLowerInvariant()}";
        var exchangeName = $"{_configuration.ExchangePrefix}.commands";
        var queueName = $"{_configuration.QueuePrefix}.{command.TargetService.ToLowerInvariant()}.commands";
        
        await DeclareExchangeAsync(exchangeName);
        await DeclareQueueAsync(queueName);
        await BindQueueAsync(queueName, exchangeName, routingKey);
        
        var message = JsonConvert.SerializeObject(command);
        var body = Encoding.UTF8.GetBytes(message);
        
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = command.MessageId.ToString();
        properties.Timestamp = new AmqpTimestamp(((DateTimeOffset)command.CreatedAt).ToUnixTimeSeconds());
        properties.Type = command.MessageType;
        properties.Priority = (byte)Math.Min(command.Priority, 255);
        properties.Headers = new Dictionary<string, object>
        {
            ["target_service"] = command.TargetService,
            ["version"] = command.Version,
            ["user_id"] = command.UserId?.ToString() ?? "",
            ["correlation_id"] = command.CorrelationId ?? ""
        };

        _channel.BasicPublish(
            exchange: exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Comando {CommandType} enviado para {TargetService} com ID {MessageId}", 
            typeof(T).Name, command.TargetService, command.MessageId);
    }

    public async Task SubscribeEventAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class, IEvent
    {
        var eventType = typeof(T).Name;
        var handlerWrapper = CreateEventHandlerWrapper(handler);
        
        _eventHandlers.TryAdd(eventType, handlerWrapper);
        
        _logger.LogInformation("Handler registrado para evento {EventType}", eventType);
        await Task.CompletedTask;
    }

    public async Task SubscribeCommandAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class, ICommand
    {
        var commandType = typeof(T).Name;
        var handlerWrapper = CreateCommandHandlerWrapper(handler);
        
        _commandHandlers.TryAdd(commandType, handlerWrapper);
        
        _logger.LogInformation("Handler registrado para comando {CommandType}", commandType);
        await Task.CompletedTask;
    }

    public async Task DeclareExchangeAsync(string exchangeName, string exchangeType = "topic", bool durable = true, bool autoDelete = false)
    {
        await EnsureConnectedAsync();
        
        _channel!.ExchangeDeclare(
            exchange: exchangeName,
            type: exchangeType,
            durable: durable,
            autoDelete: autoDelete);
            
        _logger.LogDebug("Exchange {ExchangeName} declarado", exchangeName);
    }

    public async Task DeclareQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false)
    {
        await EnsureConnectedAsync();
        
        _channel!.QueueDeclare(
            queue: queueName,
            durable: durable,
            exclusive: exclusive,
            autoDelete: autoDelete);
            
        _logger.LogDebug("Fila {QueueName} declarada", queueName);
    }

    public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey)
    {
        await EnsureConnectedAsync();
        
        _channel!.QueueBind(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey);
            
        _logger.LogDebug("Fila {QueueName} vinculada ao exchange {ExchangeName} com routing key {RoutingKey}", 
            queueName, exchangeName, routingKey);
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync();
        
        // Configurar consumo de eventos
        if (_eventHandlers.Any())
        {
            await SetupEventConsumer();
        }
        
        // Configurar consumo de comandos
        if (_commandHandlers.Any())
        {
            await SetupCommandConsumer();
        }
        
        _logger.LogInformation("Consumo de mensagens iniciado");
    }

    public async Task StopConsumingAsync(CancellationToken cancellationToken = default)
    {
        if (_channel?.IsOpen == true)
        {
            _channel.Close();
        }
        
        _logger.LogInformation("Consumo de mensagens parado");
        await Task.CompletedTask;
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        lock (_lock)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.Host,
                    Port = _configuration.Port,
                    UserName = _configuration.Username,
                    Password = _configuration.Password,
                    VirtualHost = _configuration.VirtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(_configuration.ConnectionTimeoutSeconds),
                    RequestedHeartbeat = TimeSpan.FromSeconds(_configuration.HeartbeatIntervalSeconds)
                };

                if (_configuration.UseSsl)
                {
                    factory.Ssl.Enabled = true;
                }

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                _logger.LogInformation("Conectado ao RabbitMQ em {Host}:{Port}", _configuration.Host, _configuration.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
                throw new InvalidOperationException("Falha ao conectar com o message bus", ex);
            }
        }
    }

    private async Task SetupEventConsumer()
    {
        var exchangeName = $"{_configuration.ExchangePrefix}.events";
        var queueName = $"{_configuration.QueuePrefix}.{_configuration.ServiceName}.events";
        
        await DeclareExchangeAsync(exchangeName);
        await DeclareQueueAsync(queueName);
        
        // Bind para todos os eventos que temos handlers
        foreach (var eventType in _eventHandlers.Keys)
        {
            var routingKey = $"event.{eventType.ToLowerInvariant()}";
            await BindQueueAsync(queueName, exchangeName, routingKey);
        }
        
        var consumer = new EventingBasicConsumer(_channel!);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var messageType = ea.BasicProperties.Type;
                if (_eventHandlers.TryGetValue(messageType, out var handler))
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    await handler(message, CancellationToken.None);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _logger.LogWarning("Handler não encontrado para evento {MessageType}", messageType);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    private async Task SetupCommandConsumer()
    {
        var exchangeName = $"{_configuration.ExchangePrefix}.commands";
        var queueName = $"{_configuration.QueuePrefix}.{_configuration.ServiceName}.commands";
        
        await DeclareExchangeAsync(exchangeName);
        await DeclareQueueAsync(queueName);
        
        // Bind para todos os comandos que temos handlers
        foreach (var commandType in _commandHandlers.Keys)
        {
            var routingKey = $"command.{_configuration.ServiceName.ToLowerInvariant()}.{commandType.ToLowerInvariant()}";
            await BindQueueAsync(queueName, exchangeName, routingKey);
        }
        
        var consumer = new EventingBasicConsumer(_channel!);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var messageType = ea.BasicProperties.Type;
                if (_commandHandlers.TryGetValue(messageType, out var handler))
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    await handler(message, CancellationToken.None);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _logger.LogWarning("Handler não encontrado para comando {MessageType}", messageType);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar comando");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };
        
        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
    }

    private Func<string, CancellationToken, Task> CreateEventHandlerWrapper<T>(Func<T, CancellationToken, Task> handler) where T : class, IEvent
    {
        return async (message, cancellationToken) =>
        {
            try
            {
                var eventObj = JsonConvert.DeserializeObject<T>(message);
                if (eventObj != null)
                {
                    await handler(eventObj, cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar evento {EventType}", typeof(T).Name);
                throw;
            }
        };
    }

    private Func<string, CancellationToken, Task> CreateCommandHandlerWrapper<T>(Func<T, CancellationToken, Task> handler) where T : class, ICommand
    {
        return async (message, cancellationToken) =>
        {
            try
            {
                var commandObj = JsonConvert.DeserializeObject<T>(message);
                if (commandObj != null)
                {
                    await handler(commandObj, cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar comando {CommandType}", typeof(T).Name);
                throw;
            }
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer dispose do MessageBus");
        }
        finally
        {
            _disposed = true;
        }
    }
}