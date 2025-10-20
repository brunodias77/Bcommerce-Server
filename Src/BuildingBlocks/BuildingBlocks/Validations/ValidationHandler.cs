namespace BuildingBlocks.Validations;

public class ValidationHandler
{
    private readonly List<Error> _errors = [];
    
    public IReadOnlyList<Error> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    public ValidationHandler Add(Error error)
    {
        _errors.Add(error);
        return this;
    }

    public ValidationHandler Add(string code, string message) => 
        Add(new Error(message));

    public ValidationHandler Add(string message) => 
        Add(new Error(message));

    public ValidationHandler Merge(ValidationHandler other)
    {
        _errors.AddRange(other.Errors);
        return this;
    }

    public void ThrowIfHasErrors()
    {
        if (HasErrors)
            throw new ValidationException(Errors);
    }
}