"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ContentType,
  SortOption,
  checkHealth,
  getScoreBreakdown,
  search,
  sortLabel,
  typeLabel,
  type ContentItemDto,
  type PagedResult,
  type ScoreBreakdownDto,
} from "@/lib/api";

type WakeState = "checking" | "ready" | "down";

const PAGE_SIZE = 10;

export default function HomePage() {
  const [keyword, setKeyword] = useState("");
  const [type, setType] = useState<ContentType | "">("");
  const [sort, setSort] = useState<SortOption>(SortOption.Popularity);
  const [page, setPage] = useState(1);

  const [results, setResults] = useState<PagedResult<ContentItemDto> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [wake, setWake] = useState<WakeState>("checking");
  const [breakdown, setBreakdown] = useState<ScoreBreakdownDto | null>(null);

  // Uykudaki API uyandırma yoklaması (Kanvas "uykudaki API"): sağlık gelene kadar dene.
  useEffect(() => {
    let cancelled = false;
    let attempts = 0;

    const probe = async () => {
      const ok = await checkHealth();
      if (cancelled) return;
      if (ok) {
        setWake("ready");
      } else if (attempts < 10) {
        attempts += 1;
        setWake("checking");
        setTimeout(probe, 2000);
      } else {
        setWake("down");
      }
    };

    void probe();
    return () => {
      cancelled = true;
    };
  }, []);

  const runSearch = useCallback(
    async (targetPage: number) => {
      setLoading(true);
      setError(null);
      try {
        const result = await search({
          keyword,
          type: type === "" ? null : type,
          sort,
          page: targetPage,
          pageSize: PAGE_SIZE,
        });
        setResults(result);
        setPage(targetPage);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Bilinmeyen hata");
        setResults(null);
      } finally {
        setLoading(false);
      }
    },
    [keyword, type, sort],
  );

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    void runSearch(1);
  };

  const openBreakdown = async (id: string) => {
    try {
      setBreakdown(await getScoreBreakdown(id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Skor açıklaması alınamadı");
    }
  };

  return (
    <div className="container">
      <header>
        <h1>ContentHub</h1>
        <p>Çoklu sağlayıcılı içerik arama ve puanlama</p>
      </header>

      {wake === "checking" && <div className="wake checking">Servis uyanıyor… (ücretsiz katman uyku davranışı)</div>}
      {wake === "down" && <div className="wake down">Servise ulaşılamıyor. API çalışıyor mu?</div>}
      {wake === "ready" && <div className="wake ready">Servis hazır.</div>}

      <form className="search" onSubmit={onSubmit}>
        <input
          type="text"
          placeholder="Anahtar kelime (boş bırakılabilir)"
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
          aria-label="Anahtar kelime"
        />
        <select
          value={type === "" ? "" : String(type)}
          onChange={(e) => setType(e.target.value === "" ? "" : (Number(e.target.value) as ContentType))}
          aria-label="Tür filtresi"
        >
          <option value="">Tüm türler</option>
          <option value={String(ContentType.Video)}>Video</option>
          <option value={String(ContentType.Text)}>Metin</option>
        </select>
        <select
          value={String(sort)}
          onChange={(e) => setSort(Number(e.target.value) as SortOption)}
          aria-label="Sıralama"
        >
          <option value={String(SortOption.Popularity)}>Popülerlik</option>
          <option value={String(SortOption.Relevance)}>Alakalılık</option>
          <option value={String(SortOption.Hybrid)}>Hybrid</option>
        </select>
        <button type="submit" disabled={loading}>
          {loading ? "Aranıyor…" : "Ara"}
        </button>
      </form>

      {error && <div className="error">{error}</div>}

      {results && (
        <>
          <div className="meta">
            {results.totalCount} sonuç · sayfa {results.page}/{Math.max(results.totalPages, 1)} ·{" "}
            {sortLabel(sort)} sıralaması
          </div>

          {results.items.length === 0 ? (
            <div className="empty">Eşleşen içerik yok.</div>
          ) : (
            <ul className="results">
              {results.items.map((item) => (
                <li key={item.id} className="card">
                  <div>
                    <p className="title">{item.title}</p>
                    {item.description && <p className="desc">{item.description}</p>}
                    <div className="badges">
                      <span className="badge">{typeLabel(item.type)}</span>
                      <span className="badge">{new Date(item.publishedAt).toLocaleDateString("tr-TR")}</span>
                      {item.providerCount > 1 && (
                        <span className="badge dup">{item.providerCount} sağlayıcıda mevcut</span>
                      )}
                    </div>
                  </div>
                  <div className="score">
                    <div className="value">{item.finalScore.toFixed(2)}</div>
                    <div className="label">nihai skor</div>
                    <button className="ghost" type="button" onClick={() => void openBreakdown(item.id)}>
                      skor neden?
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          <div className="pager">
            <button
              className="ghost"
              type="button"
              disabled={!results.hasPrevious || loading}
              onClick={() => void runSearch(results.page - 1)}
            >
              ← Önceki
            </button>
            <span className="meta" style={{ margin: 0 }}>
              {results.page} / {Math.max(results.totalPages, 1)}
            </span>
            <button
              className="ghost"
              type="button"
              disabled={!results.hasNext || loading}
              onClick={() => void runSearch(results.page + 1)}
            >
              Sonraki →
            </button>
          </div>
        </>
      )}

      {breakdown && (
        <div className="modal-backdrop" onClick={() => setBreakdown(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>{breakdown.title}</h2>
            <table>
              <tbody>
                <tr>
                  <td className="k">Temel puan</td>
                  <td className="v">{breakdown.baseScore.toFixed(2)}</td>
                </tr>
                <tr>
                  <td className="k">Tür katsayısı</td>
                  <td className="v">×{breakdown.typeCoefficient.toFixed(2)}</td>
                </tr>
                <tr>
                  <td className="k">Etkileşim puanı</td>
                  <td className="v">{breakdown.engagementScore.toFixed(2)}</td>
                </tr>
                <tr>
                  <td className="k">Kalıcı bileşen</td>
                  <td className="v">{breakdown.persistentScore.toFixed(2)}</td>
                </tr>
                <tr>
                  <td className="k">Güncellik (okuma anı)</td>
                  <td className="v">+{breakdown.recencyPoints}</td>
                </tr>
                <tr className="final">
                  <td>Nihai skor</td>
                  <td className="v">{breakdown.finalScore.toFixed(2)}</td>
                </tr>
              </tbody>
            </table>
            <button className="close" type="button" onClick={() => setBreakdown(null)}>
              Kapat
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
