using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// Sağlayıcılar arası tekilleştirmenin deterministik kanonik kimliği (Norms 8).
/// Üretimi <see cref="Fingerprinting.FingerprintFactory"/> tarafından yapılır.
/// </summary>
public sealed class Fingerprint : ValueObject
{
    private Fingerprint(string value) => Value = value;

    public string Value { get; }

    public static Fingerprint FromHash(string hexHash)
    {
        if (string.IsNullOrWhiteSpace(hexHash))
        {
            throw new DomainException("Fingerprint boş olamaz.");
        }

        return new Fingerprint(hexHash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
