using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Domain.Fingerprinting;

/// <summary>
/// İçerik parmak izini DETERMİNİSTİK üretir (Norms 8, S3). Semantik/bulanık değil,
/// normalize-eşleşme temellidir: aynı girdi → aynı çıktı. Saf fonksiyon; I/O yok.
///
/// Kanonik dizi = normalize(title) | tür | published_at(yyyy-MM-dd) [| normalize(sourceUrl)]
/// SHA-256 → küçük harf hex.
/// </summary>
public static class FingerprintFactory
{
    public static Fingerprint Create(
        string title,
        ContentType type,
        DateTimeOffset publishedAt,
        string? sourceUrl = null)
    {
        var normalizedTitle = Normalize(title);
        var datePart = publishedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var builder = new StringBuilder()
            .Append(normalizedTitle)
            .Append('|')
            .Append(type == ContentType.Video ? "video" : "text")
            .Append('|')
            .Append(datePart);

        if (!string.IsNullOrWhiteSpace(sourceUrl))
        {
            builder.Append('|').Append(Normalize(sourceUrl));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Fingerprint.FromHash(Convert.ToHexStringLower(hash));
    }

    /// <summary>Küçült + trim + aksan/noktalama sök + boşluk daralt. Kültür-bağımsız, deterministik.</summary>
    internal static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lowered = value.Trim().ToLowerInvariant();
        var decomposed = lowered.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue; // aksan işaretini at
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' '); // her tür ayraç/noktalama → tek boşluk
                previousWasSpace = true;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
