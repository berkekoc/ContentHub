using ContentHub.Modules.ContentSearch.Domain.Fingerprinting;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Domain.UnitTests.Fingerprinting;

public sealed class FingerprintFactoryTests
{
    private static readonly DateTimeOffset Published = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SameInput_ProducesSameFingerprint()
    {
        var a = FingerprintFactory.Create("Merhaba Dünya", ContentType.Video, Published);
        var b = FingerprintFactory.Create("Merhaba Dünya", ContentType.Video, Published);

        Assert.Equal(a, b);
        Assert.Equal(a.Value, b.Value);
        Assert.Equal(64, a.Value.Length); // SHA-256 hex
    }

    [Fact]
    public void Create_DifferentTitle_ProducesDifferentFingerprint()
    {
        var a = FingerprintFactory.Create("Alpha", ContentType.Video, Published);
        var b = FingerprintFactory.Create("Beta", ContentType.Video, Published);

        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData("Merhaba Dünya")]
    [InlineData("  merhaba   dünya  ")]
    [InlineData("MERHABA, DÜNYA!")]
    [InlineData("Merhaba---Dünya")]
    public void Create_NormalizesCaseWhitespaceAndPunctuation(string variant)
    {
        var canonical = FingerprintFactory.Create("merhaba dunya", ContentType.Video, Published);
        var candidate = FingerprintFactory.Create(variant, ContentType.Video, Published);

        // Aksan sökümü + küçültme + boşluk daraltma → aynı kanonik dizi.
        Assert.Equal(canonical, candidate);
    }

    [Fact]
    public void Create_DifferentType_ProducesDifferentFingerprint()
    {
        var video = FingerprintFactory.Create("Aynı Başlık", ContentType.Video, Published);
        var text = FingerprintFactory.Create("Aynı Başlık", ContentType.Text, Published);

        Assert.NotEqual(video, text);
    }

    [Fact]
    public void Create_SourceUrlParticipatesInIdentity()
    {
        var without = FingerprintFactory.Create("Başlık", ContentType.Video, Published);
        var with = FingerprintFactory.Create("Başlık", ContentType.Video, Published, "https://x/y");

        Assert.NotEqual(without, with);
    }

    [Fact]
    public void Create_DifferentPublishDate_ProducesDifferentFingerprint()
    {
        var day1 = FingerprintFactory.Create("Başlık", ContentType.Video, Published);
        var day2 = FingerprintFactory.Create("Başlık", ContentType.Video, Published.AddDays(1));

        Assert.NotEqual(day1, day2);
    }
}
