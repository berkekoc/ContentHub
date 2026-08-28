# ContentHub: Çok Sağlayıcılı İçerik Arama ve Puanlama Servisi

Farklı biçimlerde (JSON ve XML) veri sunan içerik sağlayıcılarını tek bir arama deneyiminde
birleştiren .NET 10 Web API'si ve üzerindeki Next.js panosu. Kullanıcı sağlayıcı ayrımını görmez;
tek bir havuzda arar, filtreler, sıralar, sayfalar. Puanlama açıklanabilir ve deterministiktir;
yeni bir sağlayıcı eklemek çekirdek koda dokunmadan yapılır.

Proje spec-driven bir süreçle geliştirildi; tasarım dokümanları `docs/spdd/` altında
(analiz, kanvas, build plan ve bağımsız review).

## Canlı demo

- API + doküman (Scalar): https://contenthub-e7xi.onrender.com/scalar
- Sağlık: https://contenthub-e7xi.onrender.com/health
- Pano: `https://<vercel-adresin>.vercel.app`  *(buraya Vercel adresini koy)*

Render ücretsiz katmanda uyuduğu için ilk istek birkaç saniye gecikebilir; pano açılışta `/health`
ile servisi uyandırır. Veri, açılışta case'in sağladığı WEG uçlarından otomatik çekilir.

## Teknoloji

.NET 10 / ASP.NET Core Minimal API, Clean Architecture + CQRS (MediatR), modüler monolit.
PostgreSQL (canlıda Supabase, bağlantı havuzu üzerinden), EF Core. Arama PostgreSQL Full-Text
Search ile (tsvector + GIN). Dağıtık cache soyutlaması (yerelde Redis, canlıda in-memory). Pano
Next.js + TypeScript (Vercel). API Docker imajıyla Render'da. Doküman OpenAPI / Scalar.

## Mimari

Bağımlılık yönü hep içe doğru. Endpoints ve Api, Application'a bağlı; Infrastructure, Application'a
bağlı; herkes Domain'e bağlı. Domain hiçbir dış kütüphaneye bağlı değil ve bu kural ArchTest'lerle
zorlanıyor.

```
Endpoints ─┐
Api ───────┼─▶ Application ─▶ Domain
Infrastructure ─▶ Application ─▶ Domain
```

Domain'de aggregate'ler, value object'ler, saf `ScoringService` (I/O yok, zaman parametre olarak
gelir) ve deterministik `FingerprintFactory` var. Application CQRS use-case'lerini ve portları
tutuyor (`IProviderAdapter`, `ISearchReadModel`, repo'lar, `IBackgroundTaskQueue`). Infrastructure
EF Core, JSON/XML adaptörleri, Polly dayanıklılık, ham SQL okuma modeli, cache ve arka plan
servislerini barındırıyor. `Modules/ContentSearch` klasörü ikinci bir modül (`provider-gateway`)
için şablon görevi görüyor.

## Puanlama

Case'in formülü birebir uygulandı:

```
Nihai Skor = (Temel Puan × Tür Katsayısı) + Güncellik Puanı + Etkileşim Puanı
```

Skor iki parçaya ayrıldı. Zamandan bağımsız kısım (temel × katsayı + etkileşim) çekim anında
hesaplanıp `content_scores.persistent_score`'a yazılıyor. Güncellik puanı saklanmıyor; okuma
anında SQL `CASE` ile ekleniyor (son 1 hafta +5, 1 ay +3, 3 ay +1, daha eski 0). SQL'deki güncellik
sınırları C# `ScoringService.RecencyPoints` ile aynı değeri üretiyor; bunu `RecencyParitySqlTests`
kilitliyor. Sıfıra bölme ya da eksik/negatif ölçükte ilgili bileşen 0 sayılıp kayıt yine listeleniyor.

## Veri kaynağı

Case iki mock uç veriyor: `.../v2/provider1` (JSON; video metrikleri views/likes) ve
`.../v2/provider2` (XML; video views/likes, makale reading_time/reactions). Bu iki sağlayıcı
`appsettings` içinden (`ContentSearch:Providers`) uygulamaya otomatik tanımlanıyor. JSON ve XML
adaptörleri onların farklı şemalarını ortak kanonik modele çeviriyor; sağlayıcı biçimi domain'e
sızmıyor (Anti-Corruption Layer).

Depodaki `mock-providers/` klasörü artık offline ve CI testleri için duruyor. WEG kümelerinde
sağlayıcılar arası kopya bulunmadığından, tekilleştirme (aynı parmak izine düşen kayıtlardan en
yüksek skorlu temsilci) WireMock'ta kasıtlı kopyalar kurulan entegrasyon testleriyle kanıtlanıyor.

## Yerel çalıştırma

Tek komutla tüm yığın (Postgres + Redis + API + pano):

```bash
docker compose up --build
```

API `http://localhost:8080`, doküman `http://localhost:8080/scalar`, pano `http://localhost:3000`.

Compose taze Postgres'te şemayı modelden kuruyor (`ContentHub__InitializeDatabase=true`), iki WEG
sağlayıcısını tanımlıyor ve zamanlanmış çekim birkaç saniye sonra veriyi dolduruyor; yani pano
kendiliğinden dolu geliyor.

Elle denemek için:

```bash
KEY="dev-local-api-key-change-me"

# Çekimi tetikle (arka plana alınır, uç anında 202 döner)
curl -X POST http://localhost:8080/api/fetch -H "X-Api-Key: $KEY"

# Çekim geçmişi (yeni/güncellenen sayısı idempotency'nin kanıtı)
curl http://localhost:8080/api/fetch-runs -H "X-Api-Key: $KEY"

# Arama (açık uç); sort 0=popülerlik, 1=alakalılık, 2=hybrid
curl "http://localhost:8080/api/search?keyword=go&sort=0&page=1&pageSize=10"
```

Manuel çekim in-process bir kuyruğa (`System.Threading.Channels`) alınıp arka plan servisiyle
işleniyor, bu yüzden uç anında 202 dönüyor. Kuyruk `IBackgroundTaskQueue` portunun arkasında;
ileride RabbitMQ ya da Hangfire'a geçmek çekirdek koda dokunmadan mümkün.

## Panoyu ayrı çalıştırma

```bash
cd dashboard
npm install
cp .env.local.example .env.local   # NEXT_PUBLIC_API_BASE_URL = API adresi
npm run dev
```

Panoda arama, tür filtresi, sıralama, offset sayfalama, "kaç sağlayıcıda mevcut" rozeti, skor
kırılımı ("skor neden?") ve açılışta servis uyandırma var. API'de CORS demo için açık; üretimde
`ContentHub:Cors:AllowedOrigins` ile daralt.

## Test

```bash
dotnet build ContentHub.sln
dotnet test            # tümü yeşil
```

Testler dört başlıkta toplanıyor: Domain birim testleri (puanlama formülünün her dalı, güncellik
sınır tarihleri, parmak izi determinizmi), Application handler testleri, mimari kurallar (ArchTest:
katman bağımlılık yönü, ScoringService saflığı, ACL biçim sızması) ve entegrasyon testleri.
Entegrasyon testleri Testcontainers ile gerçek Postgres, WireMock ile sahte sağlayıcı ayağa
kaldırır, bu yüzden Docker ister. Şema entegrasyonda `EnsureCreated` ile modelden kuruluyor
(generated `search_vector` + GIN + unique dahil), migration gerektirmiyor.

## Dağıtım

Canlı ortamda API Render'da (Docker imajı), pano Vercel'de, veritabanı Supabase (bağlantı havuzu
üzerinden). Bunun yanında depo herhangi bir Docker sunucusunda ya da yerelde tek komutla ayağa
kalkacak biçimde hazır: `Dockerfile` API imajını, `docker-compose.yml` tüm yığını (Postgres +
Redis + API + pano) kurar. Supabase yerine yerel Postgres'e dönmek istersen compose dosyası bunu
doğrudan sağlıyor.

## Bazı kararlar

- DB-agnostik tasarımın bilinçli tek istisnası arama okuma yolu (`websearch_to_tsquery`, `ts_rank`,
  `interval` aritmetiği). Postgres'e özel ve `ISearchReadModel` arkasında yalıtık; yazma, migrasyon
  ve domain DB'den bağımsız kalıyor.
- Tekilleştirme deterministik parmak iziyle: normalize(başlık) + tür + tarih (+varsa url) → SHA-256.
  Bulanık/semantik değil, normalize eşleşme; sınır bilinçli.
- Arama önek (prefix) eşleştirmeli. "clea" yazınca "clean", "API" yazınca "APIs" geliyor
  (`to_tsquery` ile `:*`), yani yazdıkça bulma hissi veriyor.
- Hybrid sıralama ağırlıkları v1 için sezgisel (`final_score` ile `ts_rank` farklı ölçekte, `scale`
  sabitiyle hizalanıyor).
- Nihai skor güncellik dahil hesaplandığından düz indeksle tam sıralanamaz; case ölçeğinde
  (birkaç yüz satır) sorun değil. Satır sayısı büyürse güncellik-kovası materyalize sütun devreye alınır.
- MediatR 12.x (Apache-2.0) hattına sabit; ücretli sürüme geçilmiyor.
