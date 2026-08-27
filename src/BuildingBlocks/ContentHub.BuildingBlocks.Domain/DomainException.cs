namespace ContentHub.BuildingBlocks.Domain;

/// <summary>Bir alan (iş) kuralının ihlalini temsil eder.</summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
