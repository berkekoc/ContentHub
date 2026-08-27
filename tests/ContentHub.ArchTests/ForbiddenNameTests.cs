using Xunit;

namespace ContentHub.ArchTests;

/// <summary>CLAUDE.md kuralı 4: yasak ad repoda hiçbir yerde geçemez (metin taraması).</summary>
public sealed class ForbiddenNameTests
{
    private static readonly string[] ScannedExtensions =
    {
        ".cs", ".csproj", ".props", ".targets", ".json", ".md", ".js", ".xml", ".yml", ".yaml", ".sln", ".txt",
    };

    // Kapsam: KOD ve teslim edilebilir artefaktlar. SPDD tasarım dokümanları (docs/) ve repo
    // yönergesi (CLAUDE.md / .claude) case'i TANIMLARKEN şirket adını anar; kural bu adın ÜRÜN
    // KODUNA sızmasını yasaklar (CLAUDE.md'nin kendisi kuralı yazarken adı içerir — repo-geneli
    // literal tarama zaten hiçbir zaman geçemez). O yüzden meta/tasarım katmanı hariç tutulur.
    private static readonly string[] ExcludedSegments =
    {
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}docs{Path.DirectorySeparatorChar}",       // SPDD tasarım dokümanları
        $"{Path.DirectorySeparatorChar}.claude{Path.DirectorySeparatorChar}",    // repo yönergesi/bağlam
        "CLAUDE.md",                                                             // kuralın kendisi adı içerir
        "ForbiddenNameTests", // bu tarama dosyasının kendisi
    };

    [Fact]
    public void Repository_MustNotContain_ForbiddenCompanyName()
    {
        // Yasak terim harf harf birleştirilir ki bu dosya kendi taramasına takılmasın.
        var forbidden = string.Concat('E', 'n', 'u', 'y', 'g', 'u', 'n');
        var root = LocateRepositoryRoot();

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (ExcludedSegments.Any(file.Contains) || !ScannedExtensions.Contains(Path.GetExtension(file)))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(file);
            }
        }

        Assert.True(offenders.Count == 0, $"Yasak ad şu dosyalarda bulundu: {string.Join(", ", offenders)}");
    }

    private static string LocateRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ContentHub.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("ContentHub.sln bulunamadı (repo kökü).");
    }
}
