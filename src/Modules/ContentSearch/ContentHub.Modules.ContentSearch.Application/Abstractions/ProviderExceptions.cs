namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>Sağlayıcı istek limitine takıldı (kalıcı 429, geri çekilmelere rağmen).</summary>
public sealed class ProviderRateLimitedException : Exception
{
    public ProviderRateLimitedException(string message)
        : base(message)
    {
    }
}

/// <summary>Sağlayıcı geçici olmayan bir hatayla erişilemez (devre kesildi).</summary>
public sealed class ProviderUnavailableException : Exception
{
    public ProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
