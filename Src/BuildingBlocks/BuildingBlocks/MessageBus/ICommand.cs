namespace BuildingBlocks.MessageBus;

/// <summary>
/// Comando - intenção de fazer algo
/// Exemplo: CreateProductCommand, PlaceOrderCommand
/// </summary>
public interface ICommand : IMessage
{
    string CommandName { get; }
}