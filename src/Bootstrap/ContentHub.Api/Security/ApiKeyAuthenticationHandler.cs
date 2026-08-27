using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ContentHub.Api.Security;

/// <summary>
/// X-Api-Key başlığını yapılandırılmış anahtarla karşılaştırır (S8). Yazma/gözlem uçları
/// bu şema ile korunur; okuma uçları açıktır.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigurationKey = "ContentHub:ApiKey";

    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
        => _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedKey) || string.IsNullOrEmpty(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredKey = _configuration[ConfigurationKey];
        if (string.IsNullOrEmpty(configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("ApiKey sunucuda yapılandırılmamış."));
        }

        if (!CryptographicEquals(providedKey!, configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Geçersiz ApiKey."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "operator") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool CryptographicEquals(string a, string b)
        => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b));
}
