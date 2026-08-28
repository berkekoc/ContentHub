# ContentHub — Çoklu Sağlayıcılı İçerik Arama ve Puanlama Servisi

> SPDD (Spec-Provided Driven Development) sürecinin **Faz D (spdd-generate)** çıktısı.
> Doğruluk kaynağı: [`docs/spdd/content-search/02-canvas.md`](docs/spdd/content-search/02-canvas.md) +
> inşa sözleşmesi: [`docs/spdd/content-search/03-build-plan.md`](docs/spdd/content-search/03-build-plan.md).

Farklı biçimlerdeki (JSON + XML) içerik sağlayıcılarını tek, tutarlı, sıralanabilir bir arama
deneyiminde birleştiren .NET 10 Web API'si. Sağlayıcı çeşitliliği kullanıcıdan gizlenir; sıralama
açıklanabilir ve tutarlıdır; yeni sağlayıcı eklemek çekirdek iş mantığına dokunmaz.

----

## ⚠️ Durum: DERLEME/TEST İLE DOĞRULANMADI

Bu kod, **.NET SDK / NuGet erişimi olmayan** bir ortamda üretildi; bu yüzden `dotnet build` ve
`dotnet test` **çalıştırılamadı**. Aşağıdaki [Doğrulama](#doğrulama) adımlarını .NET 10 SDK'lı bir
makinede çalıştırın. Derleme hatalarını düzeltmek kolaydır; en olası noktalar paket sürümleri
([`Directory.Packages.props`](Directory.Packages.props)) ve EF Core migration üretimidir.

## Kapsam (bu faz)

Build plan'daki icra sırası **O1→O13**'ün "pazarlık dışı çekirdek + yüksek getirili" kısmı
üretildi; O13'ün dağıtım kalanı bilinçli olarak **en sondan** bırakıldı (plan bunu açıkça izin verir); O12 dashboard tamamlandı.

| Adım | İçerik | Durum |
|---|---|---|
| O1 | Çözüm iskeleti (12 proje) + ArchTest kabuğu | ✅ |
| O2 | BuildingBlocks (Entity/VO/AggregateRoot, IModule, MediatR behaviors, Result/PagedResult) | ✅ |
| O3 | Domain + **saf** ScoringService + FingerprintFactory + kapsamlı birim testler | ✅ |
| O4 | Application (5 use-case, portlar, validator'lar) + handler birim testleri | ✅ |
| O5 | Persistence (DbContext, konfig, snake_case, enum→smallint, tsvector/GIN/unique) | ✅ |
| O6 | Provider entegrasyonu (JSON/XML adaptör ACL, Polly resilience, giden limit, idempotent upsert) | ✅ |
| O7 | Search okuma modeli (ham SQL: FTS + güncellik + dedup + 3 sıralama + offset) | ✅ |
| O8 | **Veri kaynağı: case'in verdiği WEG uçları** (provider1 JSON / provider2 XML) tüketilir; kendi `mock-providers/`'ımız offline/CI test double'ı olarak korunur (kopya + bozuk kayıt senaryoları) | ✅ |
| O9 | Api host (uçlar, ApiKey, ProblemDetails, gelen limit, OpenAPI/Scalar, /health) | ✅ |
| O10 | Cache (sürüm-jetonu) + zamanlanmış çekim | ✅ |
| O11 | Entegrasyon testleri (Testcontainers/WireMock) + 7 ArchTest + "yasak ad" taraması | ✅ (kod) |
| O12 | Dashboard (Next.js): arama/filtre/sıralama/sayfalama + skor açıklaması + /health uyandırma | ✅ (**npm build** doğrulandı) |
| **O13** | **Dağıtım (Render/Vercel/Supabase) + son README cilası** | ⛔ **kısmi** (Dockerfile + compose var) |

## Mimari

Clean Architecture + CQRS (MediatR) + DDD; modüler monolit. Bağımlılık yönü içe doğru.

```
Endpoints ─┐
Api ───────┼──▶ Application ──▶ Domain
Infrastructure ──▶ Application ──▶ Domain
```

- **Domain** — aggregatlar/VO'lar, `ScoringService` (saf, I/O yok, zaman parametre), `FingerprintFactory`
  (deterministik). Dış bağımlılık yasak (ArchTest ile zorlanır).
- **Application** — CQRS use-case'leri, portlar (`IProviderAdapter`, `ISearchReadModel`, repolar…), DTO'lar.
- **Infrastructure** — EF Core + Npgsql, JSON/XML adaptörleri (ACL), Polly resilience, ham SQL okuma modeli,
  cache, zamanlayıcı.
- **Endpoints/Api** — minimal API uçları + composition root; yalnız `ISender`'a konuşur.

`src/Modules/ContentSearch/` fiili **ModuleTemplate**'tir: ikinci modül (`provider-gateway`) aynı
dört-proje + Endpoints desenini taklit eder.

## Puanlama (case formülü, birebir)

```
Nihai Skor = (Temel Puan × Tür Katsayısı) + Güncellik Puanı + Etkileşim Puanı
```

- **Kalıcı** bileşen ((Temel×Katsayı)+Etkileşim) çekimde `content_scores.persistent_score`'a yazılır.
- **Güncellik** puanı SAKLANMAZ; okuma anında SQL `CASE` ile eklenir (≤7g +5, ≤1ay +3, ≤3ay +1, else 0).
  SQL sınırları C# `ScoringService.RecencyPoints` ile **birebir** (takvim ayı); `RecencyParitySqlTests`
  bu eşitliği kilitler.
- Sıfıra bölme / eksik / negatif ölçüt → ilgili bileşen **0** (kayıt yine listelenir).

## Çalıştırma (yerel, tek komut)

```bash
docker compose up --build
# API:      http://localhost:8080
# Doküman:  http://localhost:8080/scalar
# Sağlık:   http://localhost:8080/health
# (Yerel mock-providers = offline/CI test double'ı; CANLI veri case'in WEG uçlarından gelir)
```

`ContentHub__InitializeDatabase=true` compose'da şemayı modelden kurar (demo kolaylığı).

### Veri kaynağı & uçtan uca demo

Case'in verdiği iki WEG ucu **yapılandırmadan (`ContentSearch:Providers`) otomatik seed edilir**:

- `.../v2/provider1` → JSON (video: `views/likes`)
- `.../v2/provider2` → XML (video: `views/likes`, article: `reading_time/reactions`)

Zamanlanmış çekim açılıştan ~15 sn sonra çalışır → **dashboard kendiliğinden dolu gelir**. Taze bir
veritabanında (`docker compose up` ya da sıfırdan Postgres) şema modelden kurulur, sağlayıcılar seed
edilir, veri WEG uçlarından çekilir. Elle tetiklemek / gözlemlemek için:

```bash
KEY="dev-local-api-key-change-me"

# Çekimi elle tetikle (WEG uçları → kanonik model, idempotent upsert)
curl -X POST http://localhost:8080/api/fetch -H "X-Api-Key: $KEY"

# Çekim çalıştırmalarını gözlemle (yeni/güncellenen sayısı = idempotency kanıtı)
curl "http://localhost:8080/api/fetch-runs" -H "X-Api-Key: $KEY"

# Ara (açık uç): popülerlik | alakalılık | hybrid
curl "http://localhost:8080/api/search?keyword=go&sort=0&page=1&pageSize=10"

# Genişletilebilirlik kanıtı: 3. sağlayıcıyı çekirdek kurala dokunmadan ekle
curl -X POST http://localhost:8080/api/providers -H "X-Api-Key: $KEY" -H "Content-Type: application/json" \
  -d '{"name":"Ekstra Kaynak","format":0,"baseUrl":"https://.../provider1"}'
```

`format`: 0=JSON, 1=XML. `sort`: 0=Popülerlik, 1=Alakalılık, 2=Hybrid.

> **Tekilleştirme notu:** WEG veri kümelerinde sağlayıcılar-arası kopya YOKTUR; bu yüzden dedup
> (parmak izi + en yüksek skorlu temsilci) **entegrasyon/birim testleriyle** kanıtlanır (WireMock'ta
> kasıtlı kopyalar). `mock-providers/` klasörü bu offline testler ve zengin yerel demo için korunur.

### Dashboard (Next.js)

```bash
cd dashboard
npm install
cp .env.local.example .env.local   # NEXT_PUBLIC_API_BASE_URL = API adresi
npm run dev                        # http://localhost:3000
```

Arama/filtre/sıralama/sayfalama, "N sağlayıcıda mevcut" rozeti, "skor neden?" açıklaması ve
`/health` uyandırma yoklaması içerir. **`npm run build` bu ortamda doğrulandı.** API'de CORS açıktır
(demo); üretimde `ContentHub:Cors:AllowedOrigins` ile kısıtlayın.

## Doğrulama

```bash
# 1) Derleme
dotnet build ContentHub.sln

# 2) Mimari kurallar (7 kural + "yasak ad" taraması) — hızlı, Docker'sız
dotnet test tests/ContentHub.ArchTests

# 3) Domain + Application birim testleri — Docker'sız
dotnet test tests/ContentHub.Modules.ContentSearch.Domain.UnitTests
dotnet test tests/ContentHub.Modules.ContentSearch.Application.UnitTests

# 4) Entegrasyon testleri — DOCKER GEREKİR (Testcontainers Postgres + WireMock)
dotnet test tests/ContentHub.Modules.ContentSearch.IntegrationTests

# 5) Migration üret (gerçek dağıtım için; EF modeli tam yapılandırılmıştır)
dotnet ef migrations add InitialCreate \
  -p src/Modules/ContentSearch/ContentHub.Modules.ContentSearch.Infrastructure \
  -s src/Bootstrap/ContentHub.Api
```

> Migration'lar bu fazda **üretilmedi** (EF tooling çalıştırılamadı). EF konfigürasyonu eksiksizdir
> (generated `search_vector` + GIN + unique dahil); `dotnet ef migrations add` doğru migration'ı üretir.
> Entegrasyon testleri `EnsureCreated` ile şemayı modelden kurar (migration gerekmez).

## Kararlar ve kapsam dışı (özet)

- **DB-agnostik istisnası (bilinçli):** arama okuma yolu (`websearch_to_tsquery`, `ts_rank`, `interval`)
  Postgres'e özeldir; `ISearchReadModel` arkasında yalıtıktır. Yazma/migrasyon/domain DB-agnostik kalır.
- **Tekilleştirme sınırı:** deterministik "içerik parmak izi" = normalize(başlık)+tür+tarih[+url] → SHA-256.
  **Normalize eşleşme, semantik/bulanık değil** — sınır bilinçlidir.
- **Hybrid ağırlıkları** v1 sezgiseldir (`final_score` ile `ts_rank` farklı ölçekte; `scale` ile hizalanır).
- **MediatR lisansı:** 12.x (Apache-2.0) hattına **sabit**; ücretli sürüme geçilmez.
- **Ölçek eşiği:** nihai skor (güncellik dahil) hesaplanmış olduğundan düz indeksle tam sıralanamaz;
  ~250–500×2 satırda kabul edilir. ~10⁵ satırda güncellik-kovası materyalize sütun devreye alınır.
- **Devir (tech-debt):** build plan'daki E/A/S konvansiyonları `.claude/context/*` dosyalarına ayıklanmalı
  (ikinci modülün tabanı); O13 dağıtımın kalanı (canlı Render/Vercel/Supabase) tamamlanmalı.

## Sonraki faz (SPDD)

Bağımsız gözden geçirme için **YENİ oturum**:

```
spdd-review @docs/spdd/content-search/02-canvas.md @docs/spdd/content-search/03-build-plan.md
```
