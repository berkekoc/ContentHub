# Bağımsız Gözden Geçirme (Faz E): Çoklu Sağlayıcılı İçerik Arama ve Puanlama Servisi

> Modül: `content-search` · Faz: E (Review — Kanvas'a Karşı) · Tarih: 2026-08-27
> Kaynak: `docs/spdd/content-search/02-canvas.md` (Faz B) + `docs/spdd/content-search/03-build-plan.md` (Faz C)
> Gözden geçiren: bağımsız denetim (kodu üretmeyen). Salt-okunur — kod/Kanvas değiştirilmedi.
> Kapsam: `src/**`, `tests/**`, `mock-providers/**`, `dashboard/` (varlık düzeyi), migrasyonlar, ArchTests.

---

## 1. Özet Tablo (5 saniyede sağlık)

| Boyut | Durum | Not |
|---|:---:|---|
| **R — Requirements** | ✅ | 11 kabul kriterinin tamamı kodda karşılığını buluyor; puanlama/güncellik/dedup/uç durum birim+entegrasyon testli. |
| **E — Entities** | ✅ | 9 iş nesnesinin tamamı domain tipleri olarak mevcut; ilişkiler (owned 1—1, doğal anahtar, parmak izi grubu) hizalı. |
| **A — Approach** | ✅ | Ingest-then-Serve, skor kalıcı/uçucu ayrımı, üç sıralama, deterministik parmak izi, çift-tetik çekim, saf puanlama — hepsi uygulanmış. |
| **S — Structure** | ✅ | 4 katman + BuildingBlocks + Api + Endpoints; namespace/klasör/şema/tip/indeks planla birebir. (search_vector'ün EF computed-column ile üretimi — plandan olumlu sapma.) |
| **O — Operations** | ✅ | Tüm iş operasyonları doğru HTTP uçlarına eşlenmiş; koruma yüzeyi doğru; cache geçersizleştirme + zamanlayıcı yerinde. |
| **N — Norms** | ✅ | Case formülü birebir (testli), güncellik zaman-fonksiyonu, sıfır bölen kuralı, kararlı sıra, offset, idempotency, 60/dk limiti, ACL. |
| **S — Safeguards** | ✅ | 7 ArchTest kural-alanının tamamı mevcut; ApiKey + iki yönlü limit + PII-yok + skor tutarlılık sınır testi doğrulandı. Safeguard ihlali YOK. |

**Genel:** Kod, Kanvas'ın iş yüzü ve Build Plan'ın teknik yüzüyle yüksek sadakatle hizalı. Kritik bulgu yok.

---

## 2. Trafik Işığı

### 🔴 Kritik (Safeguard ihlali / çekirdek mantık uyumsuz / güvenlik)
**Yok.** Hiçbir Safeguard ihlal edilmemiş; çekirdek puanlama/dedup/idempotency mantığı Kanvas ile birebir.

### 🟡 Önemli (kısmi sapma / dikkat gereken örtük karar)
- **Y1 — Güncellik C#↔SQL eşdeğerliği, Postgres oturum saat dilimine bağımlı.** (Skor tutarlılığı Safeguard'ı; düşük olasılıklı gizli risk.)
- **Y2 — ForbiddenName (yasak ad) taraması yalnız ürün koduna kapsanmış** (docs/.claude/CLAUDE.md hariç). Build Plan "repo geneli" der; niyet karşılanıyor ama sapma bilinçli ve belgelenmeli.

### 🟢 Bilgi (stil / olumlu sapma / küçük test boşluğu)
- **B1 —** `search_vector`, ham SQL 2. migrasyon yerine EF `HasComputedColumnSql` ile (olumlu, plan varsayımını aşan çözüm).
- **B2 —** Cache sürüm-jetonu artan sayaç yerine rastgele GUID (aynı etki, daha sağlam).
- **B3 —** `provider_count` = parmak izi grup büyüklüğü (Build Plan'ın tanımı) — Kanvas R7 "kaç sağlayıcı" niyetinden nadir uçta ayrışabilir.
- **B4 —** RecencyParity testi CASE SQL'ini kopyalıyor; gerçek okuma yolunu çalıştırmıyor.
- **B5 —** ArchTest kuralı 6'nın ikinci yarısı (`IRequestHandler<>` yalnız Application'da) açıkça test edilmemiş; yalnız `sealed` doğrulanıyor.
- **B6 —** Plan listelerinde olmayan destek tipleri eklenmiş (IUnitOfWork, ProviderExceptions, ProviderAdapterBase, GlobalExceptionHandler, CORS) — modül sınırı içinde, kapsam taşması değil.

---

## 3. Karar

### ✅ Birleştirmeye hazır (dikkat notlarıyla)

Kod, çekirdek iş kurallarını, mimari sınırları ve tüm Safeguard'ları doğru uygular; kritik ya da engelleyici bulgu yoktur. **Birleştirme öncesi** yalnızca **Y1**'in (saat dilimi) bir yapılandırma/dokümantasyon adımıyla kapatılması, **Y2**'nin ise bir cümlelik gerekçe notuyla kabul edilmesi önerilir. Bunların ikisi de demo/Testcontainers bağlamında testi kırmaz; canlı (Supabase) dağıtımda tedbir amaçlıdır.

---

## 4. Detaylı Bulgular (dosya + konum)

### 🟡 Y1 — Güncellik eşdeğerliği oturum saat dilimine bağlı
**Dosya:** `src/Modules/ContentSearch/ContentHub.Modules.ContentSearch.Infrastructure/ReadModel/SearchReadModel.cs` (matched CTE, `@now - interval '1 month' / '3 months'`) ↔ `.../Domain/Scoring/ScoringService.cs` (`RecencyBandOf`, `now.AddMonths(-1)/(-3)`).

**Gözlem:** Build Plan Notes 2, "takvim ayı vs 30 gün" tuzağını doğru şekilde ele almış: C# `AddMonths` ile SQL `interval '1 month'` **ikisi de takvim ayı** semantiği kullanır. Ancak Postgres'te `timestamptz - interval '1 month'` aritmetiği **oturum `TimeZone` ayarında** yürütülür. `RecencyParitySqlTests` Testcontainers üzerinde (varsayılan UTC) koştuğu için yeşildir; fakat canlıda (Supabase) oturum saat dilimi UTC değilse, tam ay/çeyrek sınırına **çok yakın** ve subtracted pencerede bir DST geçişi olan kayıtlarda ±1 güncellik puanı sapması teorik olarak mümkündür.

**Etki:** Düşük. Yalnız band sınırına saniyeler kalmış kayıtlarda ve yalnız ±1 puanlık güncellik farkı → sıralamada mikro etki. Fakat Build Plan bu eşdeğerliği "ihlal-edilemez" ilan ettiği için işaretlenmiştir.

**Önerilen aksiyon:** Bağlantı dizesine/oturum başlangıcına `TimeZone=UTC` sabitle (Npgsql `Timezone` parametresi veya `SET TIME ZONE 'UTC'`), **ya da** gün-tabanlı aralık (`interval '30 days'/'90 days'`) yerine takvim-ay ısrarını README/tech-debt'e "UTC oturumu varsayımıyla" olarak belgele. (Kanvas/kod değişmez; bu bir dağıtım-yapılandırma notudur.)

### 🟡 Y2 — Yasak ad taraması kapsamı
**Dosya:** `tests/ContentHub.ArchTests/ForbiddenNameTests.cs` (`ExcludedSegments`: `docs/`, `.claude/`, `CLAUDE.md`).

**Gözlem:** Build Plan Safety kuralı 7 "repo genelinde 0 eşleşme" der. Gerçekte `CLAUDE.md` kuralın **kendisini** yazarken adı içerir (satır 40); `docs/spdd/01-analysis.md` ve `03-build-plan.md` de case'i tanımlarken anar. Dolayısıyla literal repo-geneli tarama **hiçbir zaman geçemez** — üretici bunu fark edip meta/tasarım katmanını bilinçle hariç tutmuş ve kod yorumunda gerekçelendirmiş.

**Doğrulama (bağımsız):** Tüm repoda `yasak ad` taraması → yalnız `CLAUDE.md` + `docs/spdd/*.md`. **Ürün kodunda (src/tests/dashboard/mock-providers) 0 eşleşme.** Safeguard'ın asıl niyeti (ad ürün koduna sızmasın) **tam karşılanıyor**.

**Önerilen aksiyon:** Kabul et; ancak test yorumundaki gerekçeyi README/tech-debt'e de bir cümleyle taşı (kural literal değil, "ürün-kodu kapsamı" olarak yorumlanmıştır).

### 🟢 B1 — search_vector üretimi (olumlu sapma)
**Dosya:** `.../Infrastructure/Persistence/Configurations/ContentItemConfiguration.cs` + `Migrations/20260827164007_InitialCreate.cs`.
Build Plan (S/Structure) "EF generated-tsvector doğrudan üretmez → 2. migrasyonda `migrationBuilder.Sql`" varsaymıştı. Üretici, EF/Npgsql'in `HasComputedColumnSql(..., stored: true)` ile bunu doğrudan yapabildiğini bulmuş; DDL doğru (`to_tsvector('simple', coalesce(title,'')||' '||coalesce(description,''))` STORED + GIN). Tek migrasyonda, daha temiz. **Örtük karar, plandan olumlu sapma.** FTS config `'simple'` hem generated column'da hem okuma SQL'inde birebir.

### 🟢 B3 — provider_count semantiği
**Dosya:** `.../Infrastructure/ReadModel/SearchReadModel.cs` (`groups` CTE, `COUNT(*)`).
`provider_count`, parmak izi grubundaki **eşleşen kayıt sayısıdır** (Build Plan O7: "grup büyüklüğü"). Aynı sağlayıcının farklı `external_id`'lerle aynı parmak izine düşen iki kaydı olursa, sayı "farklı sağlayıcı sayısı"ndan (Kanvas R7 niyeti) fazla çıkabilir. Nadir uç; Build Plan tanımıyla tutarlı. İstenirse `COUNT(DISTINCT provider_id)`'ye çevrilebilir.

### 🟢 B4 — Parity testi gerçek yolu çalıştırmıyor
**Dosya:** `tests/ContentHub.Modules.ContentSearch.IntegrationTests/RecencyParitySqlTests.cs`.
Test, güncellik CASE'ini **elle kopyalar**; `SearchReadModel`'in gerçek sorgusunu çağırmaz. Şu an ikisi birebir aynı (bağımsız doğrulandı), ama okuma modeli SQL'i ileride kayarsa test bunu yakalamaz. Öneri: parity'yi `ISearchReadModel` üzerinden veya CASE'i paylaşılan bir SQL sabitinden okuyarak sürdür.

### 🟢 B5 — ArchTest kuralı 6 kısmi
**Dosya:** `tests/ContentHub.ArchTests/PurityAndAclTests.cs` (`Handlers_ShouldBeSealed`).
Handler'ların `sealed` olduğu test edilmiş; kuralın ikinci yarısı ("`IRequestHandler<>` uygulayan tipler yalnız Application içinde") açıkça iddia edilmemiş. Küçük test boşluğu; katman testleri dolaylı korur.

---

## 5. Sapma Analizi

| Sapma türü | Bulgu | Değerlendirme |
|---|---|---|
| **Pozitif sapma** (yetkisiz ekleme) | search_vector'ün EF ile üretimi (B1); cache GUID jetonu (B2); IUnitOfWork/ProviderExceptions/CORS/GlobalExceptionHandler (B6); Provider.name → varchar(200) | Tümü modül sınırı içinde, davranışı destekliyor; **yetkisiz/riskli ekleme yok.** B1/B2 olumlu. |
| **Negatif sapma** (Kanvas'ta var, kodda yok) | **Yok** | 9 nesne, 11 kriter, 12 norm, 9 safeguard, 10 operasyon — hepsi mevcut. |
| **Yön sapması** (farklı mimari) | **Yok** | Clean Arch + CQRS + modüler monolit + ACL planla birebir. |
| **Örtük kararlar** (yetkisiz seçim) | ForbiddenName kapsamı (Y2); search_vector yöntemi (B1); cache GUID jetonu (B2); parity SQL kopyası (B4); StandardResilienceHandler ile Polly | Hepsi makul; Y2 belgelenmeli, B1/B2 iyi. Puanlama/dedup/formül gibi **çekirdekte örtük karar yok.** |
| **Kapsam taşması** (tanımlı dosya dışı) | **Yok** | Üretilen dosya ağacı Build Plan E/S iskeletiyle örtüşüyor; ek tipler tanımlı modül/katman içinde. |

---

## 6. Boyut-Bazlı Doğrulama Kanıtları

**R:** Arama/filtre/sırala/sayfala `SearchEndpoints`+`SearchReadModel`; puanlama doğruluğu `ScoringServiceTests` (video/text formül, sıfır bölen, negatif/null); güncellik tutarlılığı okuma-anı CASE + `GetScoreBreakdownQueryHandler`; dedup `mock-providers/lib/dataset.js` (24 syndicated, aynı başlık+tür+tarih+url → parmak izi); genişletilebilirlik `IProviderAdapter`+`ProviderAdapterRegistry`+`DefineProvider`; uç durum `views=0` seed + bileşen-0 kuralı; demo hacmi 24+300=324/sağlayıcı (250–500 ✓).

**E:** `Provider, ContentItem, ContentMetrics, ContentScore, Fingerprint(+FingerprintFactory), ProviderFetchRun, RateLimitPolicy, SearchContentQuery/SearchCriteria, PagedResult<ContentItemDto>`; owned 1—1 metrics/score (`ContentItemConfiguration`); doğal anahtar `ux_content_items_provider_external`.

**A:** Yazma yolu `TriggerFetchCommandHandler` (adaptör→kanonik→ComputePersistent→persistent_score→Fingerprint→idempotent upsert→ProviderFetchRun); okuma yolu `SearchReadModel` (güncellik sorgu anında); saflık ArchTest kuralı 5.

**S:** `ContentHub.Modules.ContentSearch.{Domain,Application,Infrastructure,Endpoints}` + `BuildingBlocks.*` + `Api`; 5 tablo `smallint` enum + `numeric` skor + GIN + unique + `persistent_score`/`fingerprint`/`content_type` indeksleri (`InitialCreate`).

**O:** `GET /api/search` (açık), `GET /api/content/{id}/score` (açık), `POST /api/fetch` (ApiKey), `GET /api/fetch-runs` (ApiKey), `POST /api/providers` (ApiKey), `GET /health` — `SearchEndpoints`/`IngestEndpoints`; cache invalidation `TriggerFetchCommandHandler` (anySucceeded→InvalidateAsync); zamanlayıcı `FetchSchedulerBackgroundService`.

**N:** Formül `ScoringService.ComputePersistent`/`RecencyBandOf` (testli, birebir); kararlı sıra `, r.id ASC`; offset `(Page-1)*PageSize`; idempotency `GetByNaturalKeyAsync`+unique; 60/dk `OutboundRateLimiter`; ACL `XmlProviderAdapter`/`JsonProviderAdapter` (System.Xml/Json yalnız Providers).

**S (Safeguards):** ApiKey `ApiKeyAuthenticationHandler` (FixedTimeEquals) + `RequireAuthorization(ApiKeyPolicy.Name)`; giden limit `OutboundRateLimiter`+429→`ProviderRateLimitedException`→`MarkRateLimited`; gelen limit `AddRateLimiter` (fixed window 100/dk); PII-yok seed doğrulandı (yalnız başlık/açıklama/sayaç/url); gözlemlenebilirlik `ProviderFetchRun`; skor tutarlılık `RecencyParitySqlTests`. ArchTests: LayerDependency (4 test), PurityAndAcl (3 test), ForbiddenName (1 test) — 7 kural-alanı kapsanmış.

---

## 7. Önerilen Aksiyonlar (öncelik sırasıyla)

1. **(Y1, dağıtım öncesi)** Canlı bağlantıda Postgres oturum saat dilimini `UTC`'ye sabitle **veya** takvim-ay eşdeğerliğinin "UTC oturumu" varsayımını README/tech-debt'e belgele. Kod değişikliği zorunlu değil; yapılandırma/dokümantasyon.
2. **(Y2, birleştirmeden önce)** ForbiddenName taramasının "ürün-kodu kapsamı" yorumunu README/tech-debt'e bir cümleyle taşı.
3. **(B4/B5, borç)** Parity testini gerçek okuma yoluna bağla; ArchTest kuralı 6'nın "yalnız Application" yarısını ekle.
4. **(B3, opsiyonel)** İş niyeti "kaç farklı sağlayıcı" ise `provider_count`'ı `COUNT(DISTINCT provider_id)`'ye çevir.
5. **(tech-debt, Build Plan N6)** E/A/S konvansiyonlarını `.claude/context/{patterns,architecture,module-anatomy,conventions,tech-debt}.md`'ye ayıkla (`provider-gateway` tabanı).

---

## 8. Doğrulama Notu (bu incelemenin sınırı)

Bu inceleme **statik uyum denetimidir** (kod ↔ Kanvas/Build Plan). `dotnet build`/`dotnet test` bu oturumda **bağımsız çalıştırılmamıştır**; derleme çıktıları (`bin/obj`) üreticinin ortamında derlendiğini gösterir. Build Plan'ın "yeşil test" DoD'leri Faz D/CI sorumluluğundadır. Kanvas ve Build Plan **TAMAMEN** okunmuş; her REASONS boyutu ve her Safeguard tek tek kontrol edilmiştir.

---

## Sonraki Adım

Bulgular hafif ve engelleyici değildir. İki yol:

- **Kod düzeltmesi gerekirse** (Y1 dağıtım-yapılandırması, B4/B5 test borcu): elle ya da yeni `spdd-generate` oturumu.
- **Kanvas/Build Plan güncellenmeli mi?** Y2 (ForbiddenName kapsamı) ve B3 (provider_count semantiği) kararları Build Plan'a "kabul edilen yorum" olarak yansıtılmak istenirse → **YENİ oturum** açıp `spdd-sync` çalıştırın (önce doküman güncellenir, koda dokunulmaz).
