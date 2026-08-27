# Build Plan (Teknik Yüz): Çoklu Sağlayıcılı İçerik Arama ve Puanlama Servisi

> Modül: `content-search` · Faz: C (Build Plan — Teknik Yüz) · Tarih: 2026-08-27
> Kaynak: `docs/spdd/content-search/02-canvas.md` (Faz B) + `01-analysis.md` (Faz A) + `CLAUDE.md` + `.claude/context/terminology.md`
> Bu doküman `spdd-generate`'in **birebir izleyeceği inşa sözleşmesidir**. Kod içermez; yer tutucu/TODO bırakmaz.
> İş yüzü Kanvas'ta kilitlidir; burada **yalnızca teknik yüz** tanımlanır. Kanvas ile çelişki çıkarsa **Kanvas kazanır**.

## 0. Ortam Notu — Greenfield Sapması (skill tabanından delta)

`spdd-build-plan` skill'i olgun bir repoda `.claude/context/{patterns,architecture,module-anatomy,conventions,tech-debt}.md`, `docs/spdd/_REASONS-canvas.md`, referans modül `Modules/ModuleTemplate/` ve `Tests/ArchTests/` bulunduğunu varsayar ve "tabanı tekrarlama, referans ver" der. **Bu repoda bu dosyaların hiçbiri yoktur** — yalnızca `CLAUDE.md` ve `.claude/context/terminology.md` mevcuttur (Faz A'da "greenfield" olarak saptanmıştır).

Sonuç: bu build plan referans veremez; **konvansiyonları kendisi kurar**. Bu modül aynı zamanda gelecekteki modüllerin (`provider-gateway`) taklit edeceği **referans şablondur** — yani `ModuleTemplate` rolünü fiilen bu modülün `Modules/ContentSearch/` iskeleti üstlenir. Aşağıdaki E/A/S bölümleri, ileride `.claude/context/*` dosyalarına çıkarılacak konvansiyonların ilk yazımıdır. `spdd-generate` sonrası bu konvansiyonların `.claude/context/`'e ayıklanması **N — Notes → tech-debt** altında görev olarak bırakılmıştır.

## 1. Spec + Teknoloji Tabanı (referans + delta)

Taban `CLAUDE.md`'de kilitli — burada tekrar edilmez, yalnızca teknik kararların dayandığı sürümler ve bu işe özel delta yazılır.

| Katman | Karar (taban) | Bu işe özel teknik delta |
|---|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API | Minimal API + modül başına `IModule` kayıt konvansiyonu (aşağıda A). Nullable + implicit usings açık; `TreatWarningsAsErrors=true`. |
| Mimari | Clean Architecture + CQRS (MediatR) + DDD | Tek modül `content-search`, 4 fiziksel katman projesi; `provider-gateway` kesim çizgisi hazır (S). |
| Mediator | MediatR | **Lisans kısıtı:** MediatR v12.x (Apache-2.0) sürümüne **sabitlenir**; ücretli sürüme geçilmez. Kaçış planı O/Optimizations'ta. |
| Veritabanı | PostgreSQL (Docker yerel / Supabase canlı) | EF Core migrasyonları; FTS için ham SQL migration adımı. |
| ORM | EF Core (DB-agnostic tasarım) | **Bilinçli tek Postgres bağı:** arama okuma yolu (FTS + güncellik CASE) Postgres'e özeldir; gerekçe N'de. Yazma/migrasyon yolu DB-agnostik kalır. |
| Arama | PostgreSQL FTS (tsvector + GIN) | `search_vector` generated column + GIN; `websearch_to_tsquery` + `ts_rank`. |
| Cache | `IDistributedCache` (Redis yerel / in-memory canlı) | Arama sonucu sayfası cache'lenir; **sürüm-jetonu** ile geçersizleştirme (aşağıda O). |
| Dashboard | Next.js + TypeScript (Vercel) | Ayrı dağıtım hedefi (`dashboard/`), API'yi HTTP ile tüketir; uyandırma yoklaması. |
| Mock Provider | Vercel Serverless (JSON+XML) + yerelde WireMock | Sağlayıcı sözleşmesi (sayfalama param + `429`) aşağıda O8'de tanımlı. |
| API Host | Render (Docker) | Uyku davranışı → `/health` yoklaması. |
| Doküman | OpenAPI / Scalar | .NET 10 yerleşik `Microsoft.AspNetCore.OpenApi` + Scalar UI. |

**Kütüphane sabitleri (lisans denetlenmiş):** MediatR 12.x (Apache-2.0), FluentValidation (Apache-2.0), Polly v8 `Microsoft.Extensions.Http.Resilience` (MIT), Npgsql + `Npgsql.EntityFrameworkCore.PostgreSQL` (PostgreSQL License), `System.Threading.RateLimiting` (MIT), Scalar.AspNetCore (MIT), NetArchTest.Rules (MIT), xUnit (Apache-2.0), Testcontainers / WireMock.Net (MIT / Apache-2.0). Ticari lisans gerektiren paket yoktur.

---

## E — Examples (Birebir Taklit Edilecek Somut Yapı)

`.claude/context/patterns.md` yok; kanonik desenler burada kurulur. `spdd-generate` aşağıdaki iskeleti **birebir** üretir; her yeni tür (yeni handler, yeni adaptör) bu şablonu taklit eder.

### Çözüm ve dizin ağacı (kanonik)

```
ContentHub.sln
src/
  Bootstrap/
    ContentHub.Api/                                  # host + composition root + endpoint map
  BuildingBlocks/
    ContentHub.BuildingBlocks.Domain/                # Entity, ValueObject, AggregateRoot, DomainException, IClock (arayüz)
    ContentHub.BuildingBlocks.Application/           # IModule, MediatR pipeline behaviors, Result, PagedResult<T>
    ContentHub.BuildingBlocks.Infrastructure/        # SystemClock, cache sarmalayıcı, ProblemDetails eşleme
  Modules/
    ContentSearch/
      ContentHub.Modules.ContentSearch.Domain/
      ContentHub.Modules.ContentSearch.Application/
      ContentHub.Modules.ContentSearch.Infrastructure/
      ContentHub.Modules.ContentSearch.Endpoints/    # modülün minimal API uçları (IModule impl.)
tests/
  ContentHub.Modules.ContentSearch.Domain.UnitTests/
  ContentHub.Modules.ContentSearch.Application.UnitTests/
  ContentHub.Modules.ContentSearch.IntegrationTests/
  ContentHub.ArchTests/
mock-providers/                                       # Vercel serverless (JSON + XML)
dashboard/                                            # Next.js + TypeScript
docker-compose.yml                                    # postgres + redis + wiremock + api
Dockerfile                                            # .NET API imajı (Render)
```

> `Modules/ContentSearch/` = fiili `ModuleTemplate`. İkinci modül (`provider-gateway`) tam bu dört-proje + Endpoints deseniyle açılır.

### Taklit edilecek kanonik dosya desenleri (isim + sorumluluk; kod Faz D'de)

| Desen | Kanonik örnek dosya | Sorumluluk (taklit kuralı) |
|---|---|---|
| Saf alan hizmeti | `Domain/Scoring/ScoringService.cs` | I/O yok, zaman parametre olarak alınır; her dal birim testli. |
| Value object | `Domain/Model/ContentType.cs`, `.../Fingerprint.cs` | `record`/`readonly struct`; değişmez; kendi geçerlilik kuralını taşır. |
| Aggregate | `Domain/Model/ContentItem.cs`, `.../Provider.cs` | Davranışı içeride; setter'lar `private`; doğal anahtar korunur. |
| Query + Handler | `Application/Search/SearchContent/SearchContentQuery.cs` (+ `Handler`, `Validator`) | Bir klasör = bir use-case; `IRequest`+`IRequestHandler`+`IValidator` üçlüsü. |
| Command + Handler | `Application/Ingest/TriggerFetch/TriggerFetchCommand.cs` (+ `Handler`) | Aynı use-case klasör deseni; yazma tarafı. |
| Port (arayüz) | `Application/Abstractions/IProviderAdapter.cs` | Application'da tanımlı, Infrastructure'da uygulanır. |
| Adaptör (ACL) | `Infrastructure/Providers/JsonProviderAdapter.cs`, `XmlProviderAdapter.cs` | Sağlayıcı biçimini kanonik modele çevirir; biçim tipleri (System.Text.Json/System.Xml) yalnızca burada. |
| EF yapılandırma | `Infrastructure/Persistence/Configurations/ContentItemConfiguration.cs` | Bir entity = bir `IEntityTypeConfiguration`. |
| Endpoint modülü | `Endpoints/ContentSearchModule.cs` | `IModule.RegisterServices` + `MapEndpoints`. |

**Teknik yüz — Örnekler tamam.** Somut linkler henüz yok (greenfield); yollar yukarıda kanonikleştirildi.

---

## A — Architecture (Katman ve Bağımlılık Yönü)

`.claude/context/architecture.md` yok; katman sözleşmesi burada kurulur. Bağımlılık yönü **dışa doğru yasak, içe doğru serbest**:

```
Endpoints ─┐
Api ───────┼──▶ Application ──▶ Domain
Infrastructure ──▶ Application ──▶ Domain
(Endpoints/Api ──▶ Infrastructure yalnızca composition root'ta DI kaydı için)
```

### Katman sorumlulukları (bu modül)

- **Domain** — Provider, ContentItem, ContentMetrics, ContentScore, ContentFingerprint, ProviderFetchRun, RateLimitPolicy aggregatları/VO'ları; `ScoringService` (saf); `Fingerprint` üretimi (saf, deterministik). Hiçbir dış bağımlılık: EF Core, MediatR, Npgsql, `System.Net.Http`, `System.Text.Json`, `System.Xml` **yasak**.
- **Application** — CQRS sözleşmeleri (Command/Query + Handler + Validator), portlar (`IProviderAdapter`, `IContentRepository`, `IProviderRepository`, `IFetchRunRepository`, `IProviderAdapterRegistry`, `ISearchReadModel`, `ISearchResultCache`, `IOutboundRateLimiter`, `IClock`), DTO'lar. Infrastructure'a bağımlılık **yasak**.
- **Infrastructure** — EF Core `ContentSearchDbContext` + konfigürasyonlar + migrasyonlar; repository'ler; `ISearchReadModel` (ham SQL FTS okuması); JSON/XML adaptörleri (ACL); dayanıklılık (Polly retry + circuit breaker); giden istek limiti; cache uygulaması; `FetchSchedulerBackgroundService`.
- **Endpoints** — modülün minimal API uçları; yalnızca MediatR `ISender`'a konuşur; domain'e/DbContext'e doğrudan dokunmaz.
- **Api (Bootstrap)** — composition root: `IConfiguration` bağla, modülleri (`IModule`) keşfet ve kaydet, middleware zincirini (ProblemDetails → RateLimiter → Auth → endpoints) kur.

### CLAUDE.md ihlal kuralları (bu modüle uygulanışı)

1. *"Sağlayıcı formatı asla domain modeline sızmaz (ACL)."* → JSON/XML tipleri yalnızca `Infrastructure/Providers/*`. ArchTest ile zorlanır (S bölümü).
2. *"Puanlama formülü saf fonksiyondur; I/O içermez, birim testle korunur."* → `ScoringService` Domain'de; `DateTime.Now/UtcNow`, `IClock`, repository referansı **yok** (zaman parametre). ArchTest + birim test.
3. *"Katman sınırları ArchTests ile zorlanır."* → `ContentHub.ArchTests` projesi ilk O adımında kurulur, boş değil, gerçek kurallarla.
4. *"İşveren şirketin adı (yasak ad) repoda hiçbir yerde geçmez."* → ArchTest/derleme öncesi metin taraması (S bölümü).

### Bu işe özel mimari delta

- **Okuma/yazma ayrımı fiziksel değil mantıksaldır** (tek DbContext). Fakat okuma tarafı yazma modelini (aggregate izleme) kullanmaz; `ISearchReadModel` üzerinden **projeksiyon** okur (no-tracking, ham SQL). Bu, CQRS'in bu modüldeki somut biçimidir.
- **Skorun kalıcı/uçucu ayrımı mimariye yansır:** kalıcı bileşen Infrastructure'da yazılır (çekim), güncellik bileşeni okuma SQL'inde hesaplanır. Domain her iki yarıyı da tanımlar (`ScoringService.ComputePersistent` + `ScoringService.RecencyPoints`) ki SQL, C# ile bire bir aynı sonucu versin (S/Safety'de sınır testi).

**Teknik yüz — Mimari tamam.**

---

## S — Structure (Namespace / Klasör / İskelet)

`.claude/context/{module-anatomy,conventions}.md` yok; modül anatomisi ve konvansiyonlar burada kurulur.

### Namespace kökü
`ContentHub.Modules.ContentSearch.<Layer>`; ortak: `ContentHub.BuildingBlocks.<Layer>`; host: `ContentHub.Api`.

### Modül iç anatomisi (use-case eksenli klasörleme)

```
ContentSearch.Domain/
  Model/         Provider, ContentItem, ContentMetrics, ContentScore, ContentFingerprint,
                 ProviderFetchRun, RateLimitPolicy, ContentType(VO), ProviderFormat(VO),
                 ProviderStatus(VO), ExternalId(VO), Fingerprint(VO), ScoreComponents(VO)
  Scoring/       ScoringService, ScoreComponents, RecencyBand
  Fingerprinting/ FingerprintFactory (normalize + deterministik kanonik kimlik)
  Abstractions/  (Domain-içi arayüz gerekirse; port'lar Application'da)
ContentSearch.Application/
  Abstractions/  IProviderAdapter, IProviderAdapterRegistry, IContentRepository,
                 IProviderRepository, IFetchRunRepository, ISearchReadModel,
                 ISearchResultCache, IOutboundRateLimiter, IClock
  Search/SearchContent/         Query + Handler + Validator + SearchResultDto + ContentItemDto
  Search/GetScoreBreakdown/     Query + Handler + ScoreBreakdownDto   (bonus, R'deki #5)
  Ingest/TriggerFetch/          Command + Handler + FetchSummaryDto
  Ingest/DefineProvider/        Command + Handler + Validator
  Ingest/ListFetchRuns/         Query + Handler + FetchRunDto
  Behaviors/     (BuildingBlocks'tan miras; validation + logging pipeline)
ContentSearch.Infrastructure/
  Persistence/   ContentSearchDbContext, Configurations/*, Migrations/*, Repositories/*
  ReadModel/     SearchReadModel (ham SQL FTS + güncellik CASE + dedup)
  Providers/     IProviderAdapter uygulamaları: JsonProviderAdapter, XmlProviderAdapter,
                 ProviderAdapterRegistry, ProviderHttpClient (Polly), OutboundRateLimiter
  Caching/       DistributedSearchResultCache (sürüm-jetonu)
  Scheduling/    FetchSchedulerBackgroundService
  DependencyInjection.cs (AddContentSearchInfrastructure)
ContentSearch.Endpoints/
  ContentSearchModule.cs (IModule), SearchEndpoints, IngestEndpoints
```

### Konvansiyonlar (kanonik, bu repo için ilk yazım)

- **Use-case = klasör.** Her komut/sorgu kendi klasöründe `XCommand/XQuery` + `Handler` + (varsa) `Validator` + DTO. Handler'lar `sealed`, `internal`.
- **DTO yönü:** Application dışarıya yalnızca DTO döner; aggregate/entity API yüzeyine sızmaz.
- **Repository sözleşmeleri Application'da**, uygulamaları Infrastructure'da. Yazma repository'leri aggregate döner; okuma `ISearchReadModel` DTO/projeksiyon döner.
- **Isimlendirme:** tablo/sütun `snake_case` (Npgsql konvansiyonu), C# `PascalCase`; enum'lar DB'de `smallint` (aşağıda tip tablosu) veya `text` — **`smallint` seçilir** (indeks/karşılaştırma ucuz).
- **Zaman:** her yerde `DateTimeOffset` (UTC); `IClock` yalnızca handler/servis sınırında enjekte, `ScoringService`'e parametre.

### Veri modeli — tablo şeması, tipler, indeksler (Faz C kararı)

| Tablo | Anahtar sütunlar (iş → tip) | İndeks / kısıt |
|---|---|---|
| `providers` | `id uuid PK`, `name text`, `format smallint (0=Json,1=Xml)`, `base_url text`, `status smallint (0=Active,1=Passive)`, `rate_limit_per_minute int default 60`, `overflow_behavior smallint (0=Wait,1=Retry,2=Break)` | — |
| `content_items` | `id uuid PK`, `provider_id uuid FK`, `external_id text`, `title text`, `description text`, `content_type smallint (0=Video,1=Text)`, `published_at timestamptz`, `source_url text null`, `fingerprint text`, `search_vector tsvector (generated)` | **UNIQUE `(provider_id, external_id)`** (idempotency), **GIN `(search_vector)`**, `INDEX (fingerprint)`, `INDEX (content_type)` |
| `content_metrics` | `content_item_id uuid PK/FK`, `views bigint null`, `likes bigint null`, `reading_time int null`, `reactions bigint null` | 1—1 content_items |
| `content_scores` | `content_item_id uuid PK/FK`, `base_score numeric`, `type_coefficient numeric`, `engagement_score numeric`, `persistent_score numeric`, `computed_at timestamptz` | `INDEX (persistent_score)` (popülerlik ön-eleme) |
| `provider_fetch_runs` | `id uuid PK`, `provider_id uuid FK`, `started_at`, `finished_at null`, `incoming_count int`, `new_count int`, `updated_count int`, `status smallint`, `error text null` | `INDEX (provider_id, started_at desc)` |

- **`search_vector`**: `GENERATED ALWAYS AS (to_tsvector(<config>, coalesce(title,'') || ' ' || coalesce(description,''))) STORED`; `<config>` yapılandırılabilir, varsayılan `'simple'` (stemming sürprizi olmadan deterministik) — EF migration içinde `migrationBuilder.Sql(...)` ile eklenir (EF generated-tsvector doğrudan üretmez).
- **`persistent_score` = (base × coefficient) + engagement**; güncellik **saklanmaz** (S1). Nihai skor okuma anında: `persistent_score + recency_case`.
- Ham ölçüt `nullable`; tür-özel geçerlilik domain'de (Metin'de `views` yazılmaz, Video'da `reading_time` yazılmaz — N kuralı 1).

**Teknik yüz — Yapı tamam.**

---

## O — Operations + Optimizations (Sıralı, Yürütülebilir İnşa Görevleri)

**Bu sıra icra sırasıdır.** `spdd-generate` görevleri O1→O13 sırasıyla, bağımlılık düzeninde uygular. Her görevin **Bitti tanımı (DoD)** doğrulanabilirdir. İş operasyonları (Kanvas O1–O10) bu inşa görevlerinin ürettiği uçlara eşlenir (aşağıda O9'da tablo).

> Analiz Katman sırası korunur: her adım "pazarlık dışı çekirdek" → "yüksek getirili" → "bonus" düzeninde alttan kesilebilir; sıra sabittir.

### O1 — Çözüm iskeleti + ArchTest kabuğu
Çözüm, 9 proje (4 modül + 3 BuildingBlocks + Api + ArchTests kabuğu), package referansları, `Directory.Build.props` (nullable, warnings-as-errors, LangVersion). ArchTest projesi **boş değil**: en az "Domain, Infrastructure'a bağımlı olamaz" kuralı ilk günden yeşil.
**DoD:** `dotnet build` yeşil; `dotnet test ContentHub.ArchTests` yeşil (1+ gerçek kural).

### O2 — BuildingBlocks (çekirdek)
`Entity`, `AggregateRoot`, `ValueObject`, `DomainException`, `IClock`; `IModule` (RegisterServices + MapEndpoints); MediatR pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`); `Result`, `PagedResult<T>`.
**DoD:** Behaviors DI'a bağlanabilir; birim test: `ValidationBehavior` geçersiz istekte `ValidationException` fırlatır.

### O3 — Domain + Domain birim testleri (pazarlık dışı, test yükü buraya)
Model VO/aggregatlar; `ScoringService.ComputePersistent(ContentType, ContentMetrics) : ScoreComponents`; `ScoringService.RecencyPoints(DateTimeOffset publishedAt, DateTimeOffset now) : int`; `FinalScore = persistent + recencyPoints`; `FingerprintFactory.Create(title, ContentType, publishedAt, sourceUrl?) : Fingerprint`.
- Formül birebir (N kuralı 2): Base Video `views/1000 + likes/100`, Metin `readingTime + reactions/50`; Katsayı Video 1.5 / Metin 1.0; Güncellik ≤7g +5, ≤1ay +3, ≤3ay +1, else 0; Etkileşim Video `(likes/views)*10`, Metin `(reactions/readingTime)*5`.
- **Sıfır bölen / eksik / negatif ölçüt → ilgili bileşen 0** (S5, N kuralı 4).
- Fingerprint deterministik: `normalize(title)` (küçült, trim, boşluk daralt, aksan/noktalama sök) + tür + `published_at` (`yyyy-MM-dd`) [+ source_url varsa] → SHA-256 → hex.
**DoD:** Birim testler — her formül dalı, dört güncellik aralığının **sınır tarihleri**, sıfır-bölen üç uç, fingerprint aynı girdi→aynı çıktı & farklı girdi→farklı çıktı. Tümü yeşil, deterministik.

### O4 — Application (portlar, sözleşmeler, handler'lar) + Application birim testleri
Portlar (Abstractions), DTO'lar, 5 use-case: `SearchContent`, `GetScoreBreakdown`, `TriggerFetch`, `DefineProvider`, `ListFetchRuns`. Handler'lar port'lara konuşur; FluentValidation validator'ları.
- `SearchContentQuery(keyword, contentType?, sort: Popularity|Relevance|Hybrid, page, pageSize) : PagedResult<ContentItemDto>`.
- Validator: `pageSize` üst sınır (ör. 100), `page ≥ 1`, `keyword` uzunluk sınırı.
**DoD:** Handler birim testleri (port'lar mock) — arama handler'ı doğru port çağrısı + cache-önce mantığı; TriggerFetch handler'ı adaptör→upsert→fetch-run akışını çağırır; validator sınırları test edilir.

### O5 — Infrastructure / Persistence (DbContext, konfig, migrasyon, repository)
`ContentSearchDbContext` + `IEntityTypeConfiguration` başına entity; snake_case; enum→smallint; ilk migration; **ikinci migration ham SQL**: `search_vector` generated column + GIN index + `(provider_id, external_id)` unique. Yazma repository'leri.
**DoD:** Testcontainers Postgres'e migration uygulanır; `providers/content_*` tabloları + GIN + unique index doğrulanır (entegrasyon testi).

### O6 — Infrastructure / Provider entegrasyonu (Ingest yazma yolu)
`IProviderAdapter` uygulamaları: `JsonProviderAdapter` (System.Text.Json), `XmlProviderAdapter` (System.Xml.Linq; iç içe eleman, nitelik, farklı tarih formatı — R5). `ProviderAdapterRegistry` (format→adaptör çözümü). `ProviderHttpClient`: Polly resilience (transient/429/5xx → üstel backoff retry; kalıcı hata → circuit breaker). `OutboundRateLimiter`: sağlayıcı başına 60/dk (`RateLimitPolicy`, yapılandırılabilir, S6). Çekim akışı (`TriggerFetch` handler'ının Infrastructure yanı): adaptör → kanonik ContentItem+Metrics → `ScoringService.ComputePersistent` → `persistent_score` yaz → `FingerprintFactory` → **idempotent upsert** `(provider_id, external_id)` (`INSERT ... ON CONFLICT DO UPDATE` veya EF find-or-update) → `ProviderFetchRun` kaydı.
**DoD:** Entegrasyon testi (WireMock JSON+XML uçları): iki kez çekim → kayıt sayısı sabit (idempotency), fetch_run new/updated doğru; bozuk kayıt (views=0, eksik alan) çekimi düşürmez; XML'in JSON'dan farklı şema/tarih biçimi doğru kanonikleşir.

### O7 — Infrastructure / Search okuma modeli (FTS + güncellik + dedup + sıralama + sayfalama)
`ISearchReadModel` uygulaması, **parametreli ham SQL** (`FromSqlInterpolated`/Dapper, no-tracking):
- Eşleşme: `search_vector @@ websearch_to_tsquery(<config>, @keyword)`; boş keyword → tümü.
- Tür filtresi: opsiyonel `content_type = @type`.
- Güncellik (okuma anı, S1): `persistent_score + CASE WHEN published_at >= @now - interval '7 days' THEN 5 WHEN published_at >= @now - interval '1 month' THEN 3 WHEN published_at >= @now - interval '3 months' THEN 1 ELSE 0 END AS final_score`. Sınırlar C#'taki `RecencyPoints` ile **birebir** (S/Safety).
- Alakalılık: `ts_rank(search_vector, websearch_to_tsquery(...)) AS relevance`.
- Sıralama (S2): `Popularity → ORDER BY final_score DESC`; `Relevance → ORDER BY relevance DESC`; `Hybrid → ORDER BY (@wRel*relevance + @wPop*final_score/@scale) DESC`. Ağırlık/`scale` yapılandırılabilir sabit (varsayılan wRel=0.5, wPop=0.5, scale=konfig).
- **Kararlı sıra:** her modda ikincil `, content_items.id ASC` (eşit skorda determinizm, sayfa atlama/tekrar yok — N kuralı 6).
- **Dedup (S3, N kuralı 8):** fingerprint grubunda temsilci = en yüksek final_score. SQL'de `DISTINCT ON (fingerprint)` alt-sorgusu (grup temsilcisi) → dış sorguda seçili moda göre sırala → `OFFSET/LIMIT`. Temsilci + `provider_count` (grup büyüklüğü) döner.
- Sayfalama (S7): offset tabanlı; toplam sayı ayrı `COUNT` (dedup'lı) ile → `PagedResult`.
**DoD:** Entegrasyon testleri — üç sıralama modu deterministik; aynı sorgu iki kez → aynı sıra; kopya içerik → tek temsilci + `provider_count=2`; boş sonuç hata değil boş sayfa; güncellik: yalnız `@now` değişince eşik geçen kaydın sırası düşer.

### O8 — Mock sağlayıcılar + seed
`mock-providers/`: iki Vercel serverless uç (biri JSON, biri XML), **sağlayıcı gibi** davranır: `page`/`pageSize` kabul eder, limit aşımında `429` döner; XML tarafı bilinçle zorlaştırılmış şema (iç içe, nitelik, farklı tarih/alan adı — R5). Seed: sağlayıcı başına ~250–500 içerik (S9), tür dağılımı ve kasıtlı kopya (dedup kanıtı) + bozuk kayıt (uç durum kanıtı) içerir. Yerelde WireMock eşdeğeri (Docker Compose), testler internete bağımlı değil.
**DoD:** `429` limit yolu ve sayfalama gerçek HTTP ile çalışır; seed sonrası arama dolu sonuç verir; en az bir fingerprint grubu 2 sağlayıcıya yayılır.

### O9 — Api host (uçlar + güvenlik + doküman)
Composition root: `IModule` keşif + kayıt; middleware zinciri **ProblemDetails → Incoming RateLimiter → Auth(ApiKey) → endpoints**. Uçlar (Kanvas iş operasyonu → HTTP eşlemesi):

| Kanvas iş op. | HTTP uç | Koruma |
|---|---|---|
| İçerik ara / filtre / sırala / sayfala (O1–O4) | `GET /api/search?keyword&type&sort&page&pageSize` | Açık (S8) |
| Skoru anla — bonus (O5) | `GET /api/content/{id}/score` | Açık |
| Çekimi elle tetikle (O6) | `POST /api/fetch` (opsiyonel `providerId`) | **ApiKey** |
| Çekim çalıştırmalarını gözlemle (O7) | `GET /api/fetch-runs?providerId&page` | ApiKey |
| Yeni sağlayıcı tanımla (O8) | `POST /api/providers` | **ApiKey** |
| Sağlık/uyandırma (Kanvas "uykudaki API") | `GET /health` | Açık |

ApiKey: `X-Api-Key` başlığı → authorization policy (yalnız yazma + gözlem uçları). Incoming RateLimiter: `AddRateLimiter` (sliding/fixed window, yapılandırılabilir). OpenAPI: `Microsoft.AspNetCore.OpenApi` + Scalar UI (`/scalar`).
**DoD:** ApiKey'siz yazma isteği `401/403`; okuma açık; `/scalar` tüm uçları listeler; hatalar RFC 7807 ProblemDetails; incoming limit aşımı `429`.

### O10 — Cache + zamanlanmış çekim
`DistributedSearchResultCache`: anahtar = `search:v{token}:{hash(keyword,type,sort,page,size)}`; **sürüm-jetonu** (`token`) global sayaç, başarılı `ProviderFetchRun` sonrası artırılır → eski sayfalar erişilemez olur (bayat sonuç gösterilmez, Kanvas O10). `FetchSchedulerBackgroundService` (`PeriodicTimer`, aralık yapılandırılabilir) aynı `TriggerFetch` komutunu `IServiceScopeFactory` ile çağırır (idempotent).
**DoD:** İki özdeş arama → ikincisi cache'ten (log/sayaç kanıtı); başarılı çekim sonrası aynı arama cache-miss (token değişti); background service belirtilen aralıkta çekimi tetikler (entegrasyon/log).

### O11 — Çapraz kesitler + entegrasyon testleri + ArchTests yeşil (doğrulama kapısı)
ProblemDetails uç durum eşlemeleri; yapılandırılmış günlükleme; entegrasyon test paketinin (Testcontainers/WireMock) tamamı; ArchTests tüm kurallarla yeşil (S bölümü); yasak ad metin taraması.
**DoD:** `dotnet test` (Domain+Application+Integration+Arch) tümü yeşil; yasak ad taraması 0 eşleşme.

### O12 — Dashboard (Next.js)
`GET /api/search` tüketen arama arayüzü: anahtar kelime, tür filtresi, sıralama seçici (Popülerlik/Alakalılık/Hybrid), offset sayfalama; liste **Başlık, İçerik Türü, Skor** (R10); bonus skor açıklaması (`/content/{id}/score`); açılışta `/health` uyandırma yoklaması + "servis uyanıyor" durumu (Kanvas "uykudaki API").
**DoD:** Dashboard arama→sonuç→sayfalama çalışır; uyandırma durumu gösterilir; kopya içerik "N sağlayıcıda mevcut" gösterir.

### O13 — Dağıtım + README
Dockerfile (Render), docker-compose (postgres+redis+wiremock+api tek komut), Supabase bağlantı dizesi konfig, Vercel (dashboard + mock-providers). README: dil/mimari kararlar, kurulum, kapsam dışı gerekçeleri (Elasticsearch eşiği, tekilleştirme sınırı "normalize eşleşme, semantik değil", MediatR lisans notu), canlı demo linkleri + uyku davranışı.
**DoD:** `docker compose up` ile yerel uçtan uca çalışır; canlı demo açılır.

### Optimizations (perf / maliyet / lisans kısıtları)

- **İndeksler:** GIN (FTS), unique `(provider_id, external_id)`, `fingerprint`, `content_type`, `persistent_score`. Nihai skor **hesaplanmış** (güncellik dahil) olduğu için düz indeksle tam sıralanamaz; ~250–500×2 satırda kabul edilir. **Eşik notu (README):** satır sayısı ~10⁵'e çıkarsa güncellik-kovası materyalize sütun veya periyodik yeniden puanlama devreye alınır.
- **Cache:** sayfa bazlı; sürüm-jetonu O(1) geçersizleştirme (tag desteksiz `IDistributedCache` için doğru desen).
- **Maliyet (ücretsiz katman):** veri hacmi 250–500/sağlayıcı (S9) çekimi/seed'i zorlamaz; Render uyku → `/health` yoklaması; Supabase duraklama → README'de yerel Docker Compose alternatifi.
- **Lisans:** MediatR v12.x (Apache-2.0) **sabit**; ücretli sürüme geçilmez. Kaçış planı: gerekirse BuildingBlocks'ta ince `ISender/IRequest` soyutlaması ile MediatR arkaya alınır (README'de gerekçe). Diğer paketler MIT/Apache/PostgreSQL lisansı — sorun yok.

**Teknik yüz — Operations + Optimizations tamam.**

---

## N — Notes (Bu İşe Özel Örtük Bilgi / tech-debt istisnaları)

`.claude/context/tech-debt.md` yok; bu işe özel örtük kararlar ve bilinçli borçlar burada.

1. **DB-agnostik iddiasının tek istisnası:** arama okuma yolu (`websearch_to_tsquery`, `ts_rank`, `interval` aritmetiği) Postgres'e özeldir ve **bilinçlidir** — FTS + alakalılık ek altyapısız burada. Yazma/migrasyon/domain DB-agnostik kalır. Bu istisna README'de ve ileride `tech-debt.md`'de belgelenir; kesim çizgisi: FTS okuması `ISearchReadModel` arkasında yalıtık, gerekirse başka arama motoruna taşınır.
2. **Güncellik sınırları — takvim ayı vs 30 gün:** `interval '1 month'`/`'3 months'` Postgres takvim ayı; C# tarafı `now.AddMonths(-1)`/`AddMonths(-3)` ile **aynı takvim semantiği** kullanır (30 gün sabiti DEĞİL). İkisi sınır testiyle kilitlenir (S/Safety). Bu, en kolay gözden kaçan tutarsızlık noktasıdır.
3. **FTS config `'simple'` varsayılanı:** stemming/dil sürprizini önlemek için; alakalılık kalitesi dil-özel config ile artırılabilir — yapılandırılabilir bırakıldı, varsayılan deterministik.
4. **Hybrid ağırlıkları v1 sezgiseldir:** `final_score` ile `ts_rank` farklı ölçeklerde; `scale` sabitiyle kabaca hizalanır. "Doğru" normalizasyon (sonuç-kümesi min-max) bilinçli olarak ertelendi — sayfa-içi normalizasyon sayfalamayı bozar. README'de not.
5. **Skor `numeric`:** kayan nokta sapması yerine `numeric`/`decimal`; birim test beklenen değerleri tam eşler.
6. **Konvansiyonların çıkarımı (tech-debt):** bu build plan'daki E/A/S konvansiyonları `spdd-generate` sonrası `.claude/context/{patterns,architecture,module-anatomy,conventions,tech-debt}.md` dosyalarına ayıklanmalıdır — ikinci modül (`provider-gateway`) için taban budur. Şimdilik borç olarak işaretli.
7. **Enum→smallint:** okunabilirlik yerine indeks/karşılaştırma ucuzluğu seçildi; eşleme domain'de merkezî.

**Teknik yüz — Notlar tamam.**

---

## S — Safety (İhlal-Edilemezler: ArchTest + Idempotency + Güvenlik Tabanı)

`Tests/ArchTests/` yok; ArchTest kuralları burada tanımlanır ve O1/O11'de yeşil olur.

### ArchTest kuralları (NetArchTest, ihlal = derleme/kırmızı test)

1. `Domain`, şunlara **bağımlı olamaz**: `...Application`, `...Infrastructure`, `MediatR`, `Microsoft.EntityFrameworkCore`, `Npgsql`, `System.Net.Http`, `System.Text.Json`, `System.Xml`.
2. `Application`, `...Infrastructure`'a **bağımlı olamaz**; `Npgsql`/`EntityFrameworkCore`/`AspNetCore`'a bağımlı olamaz.
3. `Endpoints` ve `Api`, `ContentSearchDbContext`'e **doğrudan** bağımlı olamaz (yalnız `ISender` + DI kaydı).
4. Sağlayıcı biçim tipleri (`System.Text.Json`, `System.Xml.*`) yalnız `Infrastructure/Providers/*` altında kullanılabilir (ACL — CLAUDE.md kuralı 1).
5. `Domain/Scoring/ScoringService`, `IClock` veya herhangi repository/HTTP tipine **referans veremez** (saflık — CLAUDE.md kuralı 2; zaman parametreyle gelir).
6. Handler'lar `sealed`; `IRequestHandler<>` uygulayan tipler yalnız `Application` içinde.
7. Metin taraması: repo genelinde **yasak ad 0 eşleşme** (CLAUDE.md kuralı 4) — CI/derleme öncesi adım.

### Idempotency (ihlal-edilemez)
Doğal anahtar `(provider_id, external_id)` **unique**; çekim upsert'tir (N kuralı 9). **Kanıt testi (O6 DoD):** aynı sağlayıcı iki kez çekilir → `content_items` sayısı sabit, `fetch_run.updated_count` doğru. Zamanlanmış + manuel çekim aynı idempotent akışı çağırır.

### Güvenlik tabanı (bu işe özel ihlal-edilemezler)
- **Yazma yüzü ApiKey** (S8, R13): `POST /api/fetch`, `POST /api/providers`, `GET /api/fetch-runs` korumalı; okuma açık. Test: korumasız yazma isteği reddedilir.
- **İki yönlü istek limiti:** giden 60/dk + backoff/circuit-breaker (S6, sağlayıcı korunur, arama ayakta); gelen limiter (açık demo kötüye kullanıma karşı). Sağlayıcı yalıtımı: bir sağlayıcının circuit-breaker'ı diğerini/aramayı etkilemez.
- **PII yok (KVKK):** mock şemaya yazar/kullanıcı adı/e-posta gibi kişisel alan **eklenmez** (R11); yalnız içerik meta verisi + toplu sayaç. ArchTest kapsamında değil, kod-inceleme + seed denetimi ile korunur.
- **Uç durum dayanıklılığı:** sıfır bölen/eksik ölçüt → bileşen 0 (S5); tek bozuk kayıt aramayı düşürmez. Test: `views=0` video etkileşim 0 ile listelenir.

### Skor tutarlılığı (yapısal güvence)
Güncellik okuma anında (S1) → skor zamanla sessizce yanlışlaşmaz. **Sınır testi (ihlal-edilemez):** C# `RecencyPoints` ile SQL güncellik CASE, aynı sınır tarihleri (`now-7g`, `now-1ay`, `now-3ay` tam sınır) için **birebir aynı** değeri üretir. Bu test kırmızıysa okuma-yazma skor tutarlılığı bozulmuş demektir.

**Teknik yüz — Safety tamam.**

---

## Boyut Özeti (Teknik Yüz)

- **E — Examples:** kanonik çözüm/dizin ağacı + 10 taklit deseni; `Modules/ContentSearch/` = fiili `ModuleTemplate`.
- **A — Architecture:** 4 katman + BuildingBlocks + Api; içe-doğru bağımlılık; CLAUDE.md 4 ihlal kuralının somut uygulaması; okuma tarafı `ISearchReadModel` projeksiyonu; skorun kalıcı/uçucu ayrımının mimariye yansıması.
- **S — Structure:** namespace kökü, use-case eksenli klasörleme, 5 tablo şeması + tipler + indeksler, `search_vector` generated column + GIN, `persistent_score` sütunu.
- **O — Operations + Optimizations:** O1→O13 icra sırası (iskelet→ArchTest→Domain→Application→Persistence→Ingest→Search→Mock/seed→Api→Cache/Scheduler→doğrulama→dashboard→dağıtım); indeks/cache/maliyet/lisans kısıtları.
- **N — Notes:** Postgres tek-bağ istisnası, takvim-ayı sınır tutarlılığı, FTS config, hybrid sezgiselliği, `numeric`, konvansiyon çıkarımı borcu.
- **S — Safety:** 7 ArchTest kuralı, idempotency kanıt testi, yazma-ApiKey + iki yönlü limit + PII-yok, C#↔SQL güncellik birebirliği sınır testi.

---

## Sonraki Faz

Bu build plan `spdd-generate`'in birebir izleyeceği inşa sözleşmesidir. Operations sırası (O1→O13) icra sırasıdır; her adımın Bitti tanımı doğrulanabilirdir.

**YENİ bir oturum** açıp şunu çalıştırın:

```
spdd-generate @docs/spdd/content-search/02-canvas.md @docs/spdd/content-search/03-build-plan.md
```
