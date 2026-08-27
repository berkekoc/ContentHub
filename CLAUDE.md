# ContentHub — Çoklu Sağlayıcılı İçerik Arama Servisi

> WEG BackEnd Developer Case Study çözümü. SPDD (Spec-Provided Driven Development) süreciyle geliştirilir.

## Bağlam
Farklı içerik sağlayıcılardan (JSON + XML) gelen verileri normalize eden, tek bir puanlama
modeline çeviren, arama/filtre/sıralama/sayfalama sunan bir API ve üzerinde basit bir dashboard.

## Teknoloji Kararları
| Katman | Seçim |
|---|---|
| Backend | .NET 10 / ASP.NET Core Web API |
| Mimari | Clean Architecture + CQRS (MediatR) + DDD, ArchTests (NetArchTest) |
| Veritabanı | PostgreSQL (yerelde Docker, canlıda Supabase) |
| ORM | EF Core (DB-agnostic tasarım) |
| Arama | PostgreSQL Full-Text Search (tsvector + GIN) |
| Cache | IDistributedCache soyutlaması — yerelde Redis, canlıda in-memory |
| Dashboard | Next.js + TypeScript (Vercel) |
| Mock Provider | Vercel Serverless Functions (JSON + XML) + yerelde WireMock |
| API Host | Render (Docker, ücretsiz katman) |
| Doküman | OpenAPI / Scalar |

## Modül Tablosu
| Modül | Rol | Durum |
|---|---|---|
| content-search | İçerik arama, puanlama, provider entegrasyonu | Faz A tamam |

## SPDD Fazları
- A — Analiz: `docs/spdd/<modül>/01-analysis.md`
- B — Kanvas: `docs/spdd/<modül>/02-canvas.md`
- C — Build Plan: `docs/spdd/<modül>/03-build-plan.md`
- D — Generate: kaynak kod
- E — Review: `docs/spdd/<modül>/04-review.md`
- F — Sync: kod↔kanvas hizalama

## Kurallar
- Sağlayıcı formatı asla domain modeline sızmaz (Anti-Corruption Layer).
- Puanlama formülü saf fonksiyondur; I/O içermez, birim testle korunur.
- Katman sınırları ArchTests ile zorlanır.
- İşveren şirketin adı (yasak ad) repoda hiçbir yerde geçmez.
