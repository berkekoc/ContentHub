using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// Dış içerik kaynağı. Yeni sağlayıcı = yeni adaptör + yapılandırma kaydı (kod dalı
/// değil, Requirements 8). İstek limiti politikasını sahiplenir (owned 1—1).
/// </summary>
public sealed class Provider : AggregateRoot<Guid>
{
    private Provider()
    {
        Name = null!;
        BaseUrl = null!;
        RateLimitPolicy = null!;
    }

    private Provider(
        Guid id,
        string name,
        ProviderFormat format,
        string baseUrl,
        ProviderStatus status,
        RateLimitPolicy rateLimitPolicy)
        : base(id)
    {
        Name = name;
        Format = format;
        BaseUrl = baseUrl;
        Status = status;
        RateLimitPolicy = rateLimitPolicy;
    }

    public string Name { get; private set; }

    public ProviderFormat Format { get; private set; }

    public string BaseUrl { get; private set; }

    public ProviderStatus Status { get; private set; }

    public RateLimitPolicy RateLimitPolicy { get; private set; }

    public static Provider Create(
        string name,
        ProviderFormat format,
        string baseUrl,
        RateLimitPolicy? rateLimitPolicy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Sağlayıcı adı boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new DomainException("Sağlayıcı erişim adresi boş olamaz.");
        }

        return new Provider(
            Guid.CreateVersion7(),
            name.Trim(),
            format,
            baseUrl.Trim(),
            ProviderStatus.Active,
            rateLimitPolicy ?? RateLimitPolicy.Default);
    }

    public void Activate() => Status = ProviderStatus.Active;

    public void Deactivate() => Status = ProviderStatus.Passive;

    public bool IsActive => Status == ProviderStatus.Active;
}
