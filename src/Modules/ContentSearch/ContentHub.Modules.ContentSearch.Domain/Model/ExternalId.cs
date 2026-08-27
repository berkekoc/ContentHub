using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>İçeriğin kaynak sistemdeki kimliği. (ProviderId, ExternalId) doğal anahtardır.</summary>
public sealed class ExternalId : ValueObject
{
    private ExternalId(string value) => Value = value;

    public string Value { get; }

    public static ExternalId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("ExternalId boş olamaz.");
        }

        return new ExternalId(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
