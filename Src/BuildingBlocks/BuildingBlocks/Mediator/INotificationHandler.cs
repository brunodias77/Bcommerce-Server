namespace BuildingBlocks.Mediator;

/// <summary>
/// Interface para handlers de Events/Notifications
/// Permite que múltiplos handlers processem o mesmo evento
/// </summary>
/// <typeparam name="TNotification">Tipo do Event/Notification</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Processa o Event/Notification de forma assíncrona
    /// </summary>
    /// <param name="notification">O Event/Notification a ser processado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}