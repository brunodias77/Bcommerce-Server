namespace BuildingBlocks.Validations;

public class ValidationException : Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public ValidationException(IReadOnlyList<Error> errors) 
        : base(CreateMessage(errors))
    {
        Errors = errors;
    }

    public ValidationException(string message) 
        : base(message)
    {
        Errors = [new Error( message)];
    }

    private static string CreateMessage(IReadOnlyList<Error> errors) =>
        errors.Count switch
        {
            0 => "Erro de validação",
            1 => errors[0].Message,
            _ => $"Múltiplos erros: {string.Join("; ", errors.Select(e => e.Message))}"
        };
}