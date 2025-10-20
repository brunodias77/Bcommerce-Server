namespace BuildingBlocks.Mediator;

/// <summary>
/// Representa um tipo vazio para Commands que não retornam valores
/// Similar ao Unit do F# ou void, mas como um tipo que pode ser usado em generics
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Instância singleton do Unit
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Verifica igualdade entre duas instâncias de Unit
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Verifica igualdade com qualquer objeto
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Retorna o hash code (sempre o mesmo para Unit)
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Representação em string do Unit
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Operador de igualdade
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Operador de desigualdade
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}