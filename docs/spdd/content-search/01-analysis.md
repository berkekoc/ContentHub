# SPDD Analiz: Çoklu Sağlayıcılı İçerik Arama ve Puanlama Servisi

> Modül: `content-search` · Faz: A (Analiz) · Tarih: 2026-08-27 · Kaynak: WEG BackEnd Developer Case Study (PDF) + iş ilanı

## Bağlam Notu — Kod Tabanı Durumu

Bu **greenfield** bir modüldür. `C:\Users\berke\sources` altında mevcut kod tabanı yoktur; repo
(`contenthub`) bu analizle birlikte açılmıştır. Dolayısıyla "mevcut modüller" taraması yerine,
adayın üretim deneyiminden gelen ve ilanla örtüşen **referans desenler** temel alınmıştır:
multi-tenant modüler monolit, Clean Architecture, CQRS (MediatR), DDD, EF Core ile DB-bağımsız
sorgulama, Redis cache, ArchTests. Modüller arası iş etkileşimi bu aşamada yoktur — tek modül
(`content-search`) mevcuttur; ancak provider entegrasyon katmanı ileride ayrı bir modüle
(`provider-gateway`) çıkarılabilecek şekilde sınırlandırılmalıdır (Faz B / Structure boyutuna girdi).

---

## Orijinal İş İhtiyacı

*(Case study PDF'inden harfiyen aktarılmıştır.)*

> **SENARYO — Arama Motoru Servisi**
>
> Farklı içerik sağlayıcılardan (provider) gelen verileri birleştirerek, kullanıcının arama sorgusuna göre en uygun içerikleri bulan, bunları belirli kriterlere göre sıralayan ve sunan bir API geliştirmeni bekliyoruz.
>
> Ekstra: Bu API üzerine basit bir dashboard arayüzü geliştirmen bekleniyor.
>
> **Teknik Gereksinimler**
>
> **API Özellikleri**
> 1. İçerik Arama ve Sıralama
>    - Anahtar kelimeye göre arama
>    - İçerik türüne (video/metin) göre filtreleme
>    - Popülerlik ve alakalılık skoruna göre sıralama
>    - Sayfalama (pagination)
> 2. İçerik Puanlama Algoritması
>    - Provider'dan gelen farklı formatlardaki verileri standart puan sistemine çevirme
>    - İçerik türüne göre ağırlık katsayıları
>    - Kullanıcı etkileşimi ve zaman bazlı güncellik puanı
>
> **Dashboard**
> - Basit bir web arayüzü geliştirmeni bekliyoruz.
> - Listeleme: Başlık, İçerik türü, Skor
> - Popülerlik/alakalılık skoru ile sıralama
>
> **Provider Entegrasyonu**
> - JSON ve XML formatlarında 2 farklı provider'dan veri alınacak
> - İstek limiti yönetimi
> - Standart formata dönüşüm
> - Yeni provider eklemeye uygun yapı
> - Verilerin veritabanında saklanması
>
> **Veri Saklama**
> - Kalıcı veri tutarlığı
> - Cache mekanizması önerisi
>
> **Teknik Beklentiler**
>
> Kod Kalitesi: Temiz ve anlaşılır kod yapısı · Hata yönetimi · Mantıklı test stratejisi · Performans ve ölçeklenebilirlik
>
> Dokümanlar: API dokümantasyonu · Kurulum ve çalıştırma talimatları (README) · Teknoloji tercih gerekçeleri
>
> Teknoloji Tercihleri — Backend: Go, PHP (Symfony), .NET Core · Veritabanı: MySQL, PostgreSQL, MongoDB
>
> Mock API'ler — Provider 1: JSON · Provider 2: XML
>
> **İçerik Puanlama Formülü**
>
> `Final Skor = (Temel Puan * İçerik Türü Katsayısı) + Güncellik Puanı + Etkileşim Puanı`
>
> Temel Puan:
> - Video: `views / 1000 + (likes / 100)`
> - Metin: `reading_time + (reactions / 50)`
>
> İçerik Türü Katsayısı: Video 1.5 · Metin 1.0
>
> Güncellik Puanı: 1 hafta içinde +5 · 1 ay içinde +3 · 3 ay içinde +1 · Daha eski +0
>
> Etkileşim Puanı:
> - Video: `(likes / views) * 10`
> - Metin: `(reactions / reading_time) * 5`
>
> **Teslim Şekli**
> - Kodları GitHub üzerinden paylaş. (Lütfen işveren şirketin adını kullanma.)
> - Özelliklerin tamamlanmasından çok, tamamlanan kısmın kaliteli olması bizim için önemli.
> - README dosyasında tercih ettiğin dil, mimari kararlar ve kurulum adımları yer almalı.
> - Ekstra özellikler ve iyileştirmeler bonus olarak değerlendirilir.

**Adayın ek kısıtı:** Çözüm .NET ağırlıklı olacak, veritabanı PostgreSQL olacak, teslim yalnızca
GitHub deposuyla sınırlı kalmayacak — Supabase (Postgres) ve Vercel (dashboard) üzerinde
**çalışan bir canlı demo** sunulacak. Süre: 2 gün.

---

## İş Alanı (Domain) Kavramları

### Mevcut nesneler
Yok — greenfield modül. Domain sıfırdan kurulur.

### Yeni nesneler

| Kavram | İş anlamı |
|---|---|
| **Provider (İçerik Sağlayıcı)** | Dış dünyadaki içerik kaynağı. Kimliği, veri formatı (JSON/XML), erişim adresi, istek limiti ve etkin/pasif durumu vardır. Yeni bir sağlayıcı **kod değişikliği değil, yapılandırma + tek bir adaptör** eklenerek sisteme girmelidir. |
| **ContentItem (İçerik)** | Sistemin kanonik içerik nesnesi. Sağlayıcı formatından bağımsız, tek bir standart temsil: başlık, açıklama/etiketler, tür (Video \| Metin), yayın tarihi, kaynak sağlayıcı ve sağlayıcıdaki kimliği. |
| **ContentMetrics (Etkileşim Ölçütleri)** | İçeriğin ham sayaçları. **Türe göre farklı ölçüt seti**: Video → `views`, `likes`; Metin → `reading_time`, `reactions`. Puanlamanın tek girdi kaynağıdır. |
| **ContentScore (Puan)** | Formülün çıktısı ve ara bileşenleri: temel puan, tür katsayısı, güncellik puanı, etkileşim puanı, nihai skor ve hesaplanma zamanı. Ara bileşenlerin saklanması "skor neden bu çıktı?" sorusunu dashboard'da açıklanabilir kılar. |
| **ProviderFetchRun (Çekim Çalıştırması)** | Bir sağlayıcıdan veri çekme işinin kaydı: ne zaman başladı, kaç kayıt geldi, kaçı yeni/güncellendi, hata/limit durumu. Gözlemlenebilirliğin ve "kalıcı veri tutarlığı" iddiasının kanıtıdır. |
| **RateLimitPolicy (İstek Limiti Politikası)** | Sağlayıcı başına birim zamanda izin verilen istek sayısı ve limit aşımında davranış (bekle / yeniden dene / devreyi kes). |
| **SearchQuery / SearchResult** | Kullanıcının arama niyeti (anahtar kelime, tür filtresi, sıralama ölçütü, sayfa) ve buna karşılık gelen sıralı, sayfalanmış sonuç kümesi. |

### İlişkiler ve temel iş kuralları

1. **Provider 1—N ContentItem.** Bir içerik tam olarak bir sağlayıcıya aittir.
2. **Doğal anahtar `(ProviderId, ExternalId)`.** Çekim işlemi *idempotent* olmalıdır: aynı içerik
   tekrar çekildiğinde kopya yaratmaz, günceller. "Kalıcı veri tutarlığı" gereksiniminin karşılığı budur.
3. **ContentItem 1—1 ContentScore.** Skor türetilmiş bir değerdir; ham ölçütler kaybolmadan saklanır
   ki formül değiştiğinde geçmiş veri yeniden puanlanabilsin.
4. **İçerik türü, ölçüt setini ve formül dalını belirler.** Video içerikte `reading_time` anlamsızdır,
   metin içerikte `views` anlamsızdır. Bu, modelde tür-özel bir geçerlilik kuralıdır — ölçütlerin
   "hepsi nullable tek tablo" olarak savrulmasına izin verilmemelidir.
5. **Güncellik puanı zamanın fonksiyonudur.** Aynı içeriğin skoru, veri hiç değişmese bile bir hafta
   sonra düşer (+5 → +3). Bu, sistemin en kritik ve en kolay gözden kaçan iş kuralıdır: skor
   *saklanabilir bir olgu* değil, *zamana bağlı bir görüştür*.
6. **Popülerlik ≠ Alakalılık.** Popülerlik arama teriminden bağımsız, içeriğin kendi skorudur.
   Alakalılık ise arama terimine göre metin eşleşme kuvvetidir. İkisi ayrı sıralama ölçütleridir
   ve case ikisini de istemektedir.
7. **Formülün tanımsız uçları vardır.** `views = 0` olan bir video (`likes/views`), `reading_time = 0`
   olan bir metin (`reactions/reading_time`) sıfıra bölme üretir. Sağlayıcı verisi negatif veya
   eksik gelebilir. Bu uç durumların domain düzeyinde tanımlı bir cevabı olmalıdır (puan 0 kabul
   edilir), yoksa üretimde çalışan bir arama servisi tek bir bozuk kayıtla çöker.
8. **Sağlayıcı formatı domain'e sızmaz.** JSON'daki `view_count` ile XML'deki `<Views>` aynı domain
   alanına düşer; dönüşüm tek bir yerde (adaptör) yapılır ve domain bu iki dünyadan habersizdir.
9. **Tekilleştirme kapsam dışıdır (varsayım).** Aynı içeriğin iki farklı sağlayıcıda bulunması
   durumunda birleştirme yapılmaz; her sağlayıcı kaydı ayrı bir `ContentItem`'dır. Bu bilinçli bir
   kapsam kararıdır (bkz. Açık Sorular).

---

## Stratejik Yaklaşım

### Çözüm yönü: "Önce topla, sonra sun" (Ingest-then-Serve)

Arama isteği geldiğinde sağlayıcılara canlı gidilmez. Veri **ayrı bir çekim akışında** toplanır,
normalize edilir, puanlanır ve PostgreSQL'e yazılır; arama yalnızca kendi veritabanımızı sorgular.

Bunun gerekçesi üç iş kısıtıdır: (a) case açıkça "verilerin veritabanında saklanması" ve "kalıcı veri
tutarlığı" istiyor; (b) sağlayıcıların istek limiti var — arama trafiğini sağlayıcıya birebir yansıtmak
limiti ilk gerçek kullanımda patlatır; (c) sağlayıcı yavaş ya da ölü olduğunda arama servisi ayakta
kalmalıdır. Ayrım aynı zamanda sistemi iki bağımsız ölçeklenme eksenine böler: okuma tarafı (arama)
ve yazma tarafı (çekim).

### Temel iş kararları

1. **Sağlayıcı bağımsızlığı bir sözleşmeyle sağlanır.** Tüm sağlayıcılar tek bir ortak arayüz
   ardında durur; JSON/XML farkı yalnızca o sağlayıcının adaptöründe yaşar. Yeni sağlayıcı eklemek =
   yeni bir adaptör sınıfı + yapılandırma kaydı. Case'in "yeni provider eklemeye uygun yapı"
   maddesinin ölçülebilir karşılığı: **çekirdek kodda tek satır değişiklik olmadan** üçüncü sağlayıcı
   eklenebilmeli.
2. **Puanlama saf bir alan hizmetidir.** Formül veritabanına, HTTP'ye ya da zamana doğrudan
   dokunmaz; zaman dışarıdan verilir. Böylece formülün her dalı (video/metin, dört güncellik
   aralığı, sıfır bölen uçları) deterministik birim testlerle kilitlenir. Case'in "mantıklı test
   stratejisi" beklentisine verilecek en güçlü cevap budur — formül sistemin iş değeridir, test
   yükü oraya yığılmalıdır.
3. **Skor iki parçaya ayrılır: kalıcı ve uçucu.** Temel puan, tür katsayısı ve etkileşim puanı
   yalnızca ham ölçütlere bağlıdır — çekim/güncelleme anında hesaplanıp saklanır. Güncellik puanı
   ise zamana bağlıdır ve saklandığı anda bayatlar. Önerilen yaklaşım: **kalıcı bileşen sütunda,
   güncellik bileşeni sorgu anında** hesaplanır; böylece sıralama her zaman doğru olur ve gece
   yarısı toplu yeniden puanlama işine gerek kalmaz. (Alternatif: periyodik yeniden puanlama işi —
   basit ama skorun 24 saate kadar yanlış kalmasına izin verir.) Bu karar A→B kapısında teyit
   edilmelidir.
4. **Arama motoru olarak PostgreSQL yeterlidir.** Full-text search (`tsvector` + GIN indeks)
   anahtar kelime aramasını ve `ts_rank` üzerinden **alakalılık skorunu** doğrudan verir. Bu, case'in
   "popülerlik ve alakalılık skoruna göre sıralama" maddesini ek altyapı olmadan karşılar.
   Elasticsearch bilinçli olarak **kapsam dışı** bırakılmıştır: bu veri hacminde ölçülebilir bir
   fayda getirmezken, ikinci bir kalıcı bileşen, ayrı bir dağıtım ve senkronizasyon tutarlılığı
   problemi ekler. README'de "hangi eşikte Elasticsearch'e geçilir" sorusu ölçütleriyle yazılır —
   teknoloji-agnostik yaklaşımın kanıtı, aracı kullanmak değil, **ne zaman gerekmediğini
   gerekçelendirebilmektir**.
5. **İstek limiti iki yönlüdür.** Giden yön: sağlayıcıya yapılan çağrılar oranlanır, geçici
   hatalarda geri çekilerek yeniden denenir, kalıcı hatada devre kesilir. Gelen yön: kendi
   API'mize de bir limit uygulanır — canlı ve herkese açık bir demo yayınlandığı için bu güvenlik
   gereksinimidir, süs değil.
6. **Cache bir soyutlama olarak girer.** Case yalnızca "cache mekanizması önerisi" istiyor; buna
   rağmen mekanizma gerçekten kurulur ama sağlayıcısı değiştirilebilir bırakılır: yerel/Docker
   ortamında Redis, ücretsiz canlı ortamda bellek içi. Böylece hem öneri hem çalışan kanıt sunulur,
   üstelik demo maliyeti sıfır kalır. Cache'lenecek şey arama sonucu sayfasıdır; geçersizleştirme
   tetikleyicisi başarılı bir çekim çalıştırmasıdır.
7. **Ham veri atılmaz.** Sağlayıcıdan gelen ölçütler saklanır. Formül değişirse (ki case'in verdiği
   katsayılar tipik olarak ürün kararıdır ve değişir) geçmiş veri yeniden puanlanabilir. Yalnızca
   nihai skoru saklamak, geri dönüşü olmayan bir veri kaybıdır.

### Teslim ve dağıtım kararı (adayın ek kısıtı)

Teslim yalnızca bir GitHub deposu değil, **açılıp denenebilen canlı bir sistem** olacaktır. Bunun
iş değeri şudur: değerlendiren kişi depoyu klonlamadan, bağımlılık kurmadan ürünü görebilir.

Seçilen topoloji ve gerekçeleri:

| Bileşen | Ortam | Gerekçe |
|---|---|---|
| PostgreSQL | **Supabase** ücretsiz katman | Kalıcı, süresiz (Render'ın ücretsiz Postgres'i 30–90 günde siliniyor), yönetilen yedekleme, hazır bağlantı dizesi. |
| .NET API | **Render** ücretsiz katman (Docker) | Ücretsiz katmanı 2026'da hâlâ gerçek: 750 saat/ay, 512 MB RAM, Dockerfile ile doğrudan dağıtım, kredi kartı istemiyor. Karşılığı: 15 dk trafiksizlikten sonra uykuya geçiyor, uyanması ~30–50 sn. |
| Dashboard | **Vercel** (Next.js + TypeScript) | Vercel'in doğal formatı; ilan TypeScript'i de sayıyor, bu da "tek yığına bağlı değilim" mesajını somutlaştırıyor. |
| Mock sağlayıcılar | **Vercel Serverless Functions** (JSON + XML uçları) | Aşağıda gerekçelendirilmiştir. |

**Vercel neden API'yi barındıramaz:** Vercel yalnızca Node/serverless çalışma zamanı sunar, uzun
ömürlü bir .NET süreci çalıştıramaz. Bu yüzden API ayrı bir konteyner platformuna gider.

**Render'ın uyku sorunu ve çözümü:** İlk istek yavaş olacaktır. Bu gizlenmez, **yönetilir**:
dashboard açılışında API'ye bir sağlık yoklaması gönderilir ve kullanıcıya "servis uyanıyor"
durumu gösterilir; README'de bu davranış ve nedeni açıkça yazılır. Ücretsiz altyapının bilinen bir
kısıtını fark edip arayüzde ele almak, değerlendiricinin gözünde bir eksi değil, mühendislik
olgunluğu işaretidir.

**Mock sağlayıcılar nerede duracak — önerilen çözüm:** İki mock sağlayıcı, dashboard ile aynı
Vercel projesi içinde iki serverless uç olarak yayınlanır (biri JSON, biri XML döner). Bunun üç
nedeni var: (a) uyanma gecikmesi yoktur — sağlayıcının "her zaman ayakta" olması gerçek dünyaya
uygundur, uykudaki bir sağlayıcı hata ayıklamayı zehirler; (b) ek bir dağıtım hedefi, ek bir ücretsiz
hesap ve ek bir cold start yoktur; (c) gerçek bir HTTP çağrısı olduğu için istek limiti, yeniden
deneme ve devre kesici mantığı **gerçekten** çalışır — dosyadan okumak bu katmanı sahte bırakırdı.
Mock uçları ayrıca kasıtlı olarak sağlayıcı gibi davranır: sayfalama parametresi kabul eder ve limit
aşımında `429` döner, böylece istek limiti yönetimi gösterilebilir hale gelir. Yerel geliştirme ve
entegrasyon testleri için ayrıca Docker Compose içinde WireMock kullanılır — testler internete
bağımlı olmamalıdır.

### Değerlendirilen alternatifler

| Alternatif | Neden seçilmedi |
|---|---|
| Her aramada sağlayıcılara canlı gitmek (fan-out) | Sağlayıcı istek limitini arama trafiğine bağlar, gecikmeyi en yavaş sağlayıcıya kilitler, "veritabanında saklama" gereksinimini karşılamaz. |
| MongoDB | Veri güçlü şekilde ilişkisel (sağlayıcı → içerik → ölçüt → skor) ve tutarlılık açıkça isteniyor. Ayrıca Supabase Postgres'i ücretsiz ve yönetilen olarak veriyor; adayın PostgreSQL derinliği de burada. |
| Elasticsearch ile arama | Bu hacimde fayda yok, kurulum ve senkronizasyon maliyeti var, ücretsiz demo topolojisine sığmıyor. Geçiş eşiği README'de gerekçelendirilir. |
| Skoru tamamen sorgu anında hesaplamak | Sıralama ve sayfalama veritabanında yapılamaz hale gelir; tüm tabloyu belleğe çekmek gerekir — ölçeklenmez. |
| Skoru tamamen saklamak (güncellik dahil) | Skor zamanla sessizce yanlışlaşır; düzeltmek için toplu yeniden puanlama işi gerekir. |
| Mock sağlayıcıları repo içinde statik dosya yapmak | Entegrasyon katmanı (HTTP, limit, yeniden deneme, hata yönetimi) sahte kalır — case'in tam olarak ölçmek istediği yer boşa çıkar. |
| Fly.io / Railway'de API barındırmak | Fly.io ücretsiz katmanı 2024'te kapandı, kredi kartı zorunlu. Railway kredi bazlı ve tükenince demo ölür. Render kredi kartı istemeden kalıcı olarak ücretsiz. |
| Koyeb'de API barındırmak | Ücretsiz katman 0.1 vCPU veriyor — .NET soğuk başlangıcı için pratikte yetersiz; ayrıca kredi kartı doğrulaması istiyor. |
| Blazor WASM dashboard | Tek dil avantajı var ama Vercel'in doğal formatı değil ve ilanın TypeScript sinyalini karşılamıyor. |

---

## Risk ve Açık Sorular

| # | Risk / belirsizlik | Etki | Kim cevaplar |
|---|---|---|---|
| R1 | **Güncellik puanı zamana bağlı** — skorun ne zaman hesaplandığı tanımlanmazsa sıralama tutarsız olur; sayfa 2'de sayfa 1'deki kayıt tekrar görünebilir. | Yüksek — ürünün ana çıktısı yanlışlanır | Aday (mimari karar), Faz B'de kesinleşir |
| R2 | **Formülün tanımsız uçları** — `views=0`, `reading_time=0`, negatif/eksik ölçüt. Sıfıra bölme tüm arama isteğini düşürebilir. | Yüksek — üretim çökmesi | Aday (varsayım: bölen 0 ise ilgili puan 0) |
| R3 | **"Alakalılık" tanımı case'te yok** — yalnızca metin eşleşme kuvveti mi, yoksa popülerlikle harmanlanmış hibrit bir skor mu? | Orta — sıralama davranışını belirler | WEG (sorulamıyorsa aday varsayımı dokümante eder) |
| R4 | **Aynı içeriğin iki sağlayıcıda bulunması** — tekilleştirme yapılmazsa sonuçlarda görünür kopya oluşur. | Orta — kullanıcı deneyimi | Aday (varsayım: v1'de tekilleştirme yok, gerekçesi yazılır) |
| R5 | **Mock sağlayıcı şemasını biz tasarlıyoruz** — kendimize kolay veri yazma riski. XML tarafı bilinçli olarak zorlaştırılmazsa (iç içe elemanlar, nitelikler, farklı tarih formatı, farklı alan adları) entegrasyon katmanı gerçekçiliğini yitirir. | Orta — case'in ölçtüğü yeteneği zayıflatır | Aday (tasarım kararı) |
| R6 | **Render ücretsiz katmanı 15 dk sonra uykuya geçiyor** (~30–50 sn uyanma). Değerlendirici linke tıkladığında ilk istek zaman aşımına uğrayabilir. | Orta — ilk izlenim | Aday (dashboard'da uyandırma + README notu) |
| R7 | **Supabase ücretsiz projesi uzun inaktivitede duraklatılabilir.** Teslimden sonra değerlendirme gecikirse demo ölü görünür. | Orta — teslim sonrası | Aday (teslim öncesi kontrol + README'de yerel Docker Compose alternatifi) |
| R8 | **İstek limiti değerleri case'te verilmemiş** (sağlayıcı başına X istek/dk). | Düşük — varsayımla kapanır | Aday (yapılandırılabilir yapılır, varsayılan dokümante edilir) |
| R9 | **2 günlük süre / maksimum kapsam gerilimi.** Case açıkça "tamamlanan kısmın kaliteli olması" diyor; yarım kalmış bir CI hattı, eksiksiz bir çekirdekten daha kötü sinyal verir. | Yüksek — teslimin bütünlüğü | Aday (kapsam kararı, aşağıdaki Ek'te) |
| R10 | **Çekim tetikleyicisi tanımsız** — manuel uç mu, zamanlanmış iş mi, ilk aramada tembel çekim mi? Demo'da veri "nasıl oraya geliyor" sorusunun cevabı. | Orta — demo anlatısı | Aday, Faz B'de kesinleşir |
| R11 | **KVKK / PII** — bu modülde kişisel veri işlenmiyor (içerik meta verisi ve toplu sayaçlar). Yazar/kullanıcı adı gibi alanlar mock şemaya eklenirse durum değişir. | Düşük | Aday (mock şemada kişisel alan tutulmaz) |
| R12 | **Kiracı (tenant) izolasyonu** — case tek kiracılı. Adayın deneyimi çok kiracılı olsa da buraya çekilmesi gereksiz karmaşıklıktır. | Düşük — kapsam dışı bırakılır | Aday (bilinçli kapsam kararı) |
| R13 | **Herkese açık demo, açık API** — kötüye kullanım, maliyet ve veri bozulması riski. Yazma uçları (çekim tetikleme) korumasız kalmamalı. | Orta — güvenlik | Aday (yazma uçlarına ApiKey + gelen istek limiti) |

**Cevap bekleyen açık sorular (A→B kapısı):**

- [ ] **S1 (R1):** Skor hangi modelle sunulacak — kalıcı bileşen sütunda + güncellik sorgu anında (önerilen), yoksa periyodik toplu yeniden puanlama mı?
- [ ] **S2 (R3):** "Alakalılık" ile "popülerlik" ayrı iki sıralama seçeneği mi olacak, yoksa ağırlıklı tek bir hibrit skor mu? (Öneri: ikisi ayrı seçenek + `hybrid` üçüncü seçenek olarak bonus.)
- [ ] **S3 (R4):** Sağlayıcılar arası tekilleştirme v1 kapsamında mı? (Öneri: hayır, gerekçesi README'ye.)
- [ ] **S4 (R10):** Çekim nasıl tetiklenecek — korumalı manuel uç, uygulama içi zamanlanmış iş, yoksa ikisi birden? (Öneri: ikisi birden; demo için manuel uç şart.)
- [ ] **S5 (R2):** Sıfır bölen ve eksik ölçüt durumunda kural "ilgili puan bileşeni 0" olarak mı sabitlensin? (Öneri: evet, ve bu kural birim testle kilitlenir.)
- [ ] **S6 (R8):** Sağlayıcı istek limiti varsayılanı ne olsun? (Öneri: sağlayıcı başına 60 istek/dk, yapılandırılabilir.)
- [ ] **S7:** Sayfalama modeli offset tabanlı mı, keyset (cursor) tabanlı mı? (Öneri: dashboard için offset yeterli; keyset'e geçiş gerekçesi README'de.)
- [ ] **S8:** Dashboard'da kimlik doğrulama olacak mı? (Öneri: okuma uçları açık, yazma uçları ApiKey.)
- [ ] **S9:** Demo veri hacmi ne olsun? (Öneri: sağlayıcı başına ~250–500 içerik — sayfalama ve indeks etkisini görünür kılacak kadar, çekimi yavaşlatmayacak kadar.)

---

## Ek: Kapsam ve 2 Günlük Zaman Değerlendirmesi

Adayın tercihi "maksimum kapsam" yönünde; ancak case metni bunun tersini ödüllendiriyor:
*"Özelliklerin tamamlanmasından çok, tamamlanan kısmın kaliteli olması bizim için önemli."*
Bu cümle bir nezaket ifadesi değil, **değerlendirme ölçütüdür**. Yarım kalmış bir CI hattı ya da
boş bir Elasticsearch bağımlılığı, eksiksiz bir çekirdekten daha kötü sinyal verir.

Önerilen ayrım — **maksimum kapsam hedeflenir, ancak sıra sabittir ve süre biterse alttan kesilir**:

**Katman 1 — Pazarlık dışı (bunlar olmadan teslim edilmez)**
Domain + puanlama motoru ve birim testleri · iki sağlayıcı adaptörü (JSON + XML) · idempotent çekim ·
PostgreSQL şeması ve migrasyonlar · arama/filtre/sıralama/sayfalama API'si · OpenAPI dokümanı ·
Docker Compose ile tek komutla yerel çalıştırma · README (dil tercihi, mimari kararlar, kurulum) ·
temel dashboard (başlık, tür, skor, sıralama).

**Katman 2 — Yüksek getirili, kapsamda tutulur**
İstek limiti (giden + gelen) · yeniden deneme ve devre kesici · cache soyutlaması ve geçersizleştirme ·
merkezî hata yönetimi (ProblemDetails) · ArchTests · entegrasyon testleri (Testcontainers veya WireMock) ·
canlı dağıtım (Supabase + Render + Vercel).

**Katman 3 — Bonus, yalnızca Katman 1–2 bittiyse**
GitHub Actions CI · sağlık yoklaması ve yapılandırılmış günlükleme · skor bileşenlerinin dashboard'da
açıklanması ("bu skor neden bu?") · üçüncü bir sağlayıcı ekleyerek genişletilebilirliğin kanıtlanması.

**Katman 4 — Bilinçli olarak kapsam dışı (README'de gerekçesiyle yazılır)**
Elasticsearch · çok kiracılılık · kullanıcı kimlik yönetimi · sağlayıcılar arası tekilleştirme ·
gerçek zamanlı güncelleme (WebSocket/SSE).

**Kaba zaman dağılımı (2 gün):** Gün 1 — veritabanı şeması ve domain, puanlama motoru ve testleri,
sağlayıcı adaptörleri ve çekim akışı (Katman 1'in backend kısmı). Gün 2 — arama API'si ve dokümanı,
Katman 2, dashboard, dağıtım ve README. Katman 3'e ancak Gün 2'nin son diliminde girilir.

**Değerlendirme:** Katman 1 + 2 iki güne SPDD ve yapay zekâ destekli akışla sığar. Katman 3'ün
tamamı sığmaz — CI ve sağlık yoklaması ucuzdur, diğerleri opsiyoneldir. Kapsam dışı bırakılan her
maddenin README'de **gerekçelendirilmiş** olması, ilanın "doğru aracı seçme, tek yığına bağlı
olmama" beklentisine verilen doğrudan cevaptır.

---

## Sonraki Faz

Yukarıdaki **A→B kapısı** sorularını (S1–S9) cevapladıktan sonra **YENİ bir oturum** açın ve şunu çalıştırın:

```
spdd-canvas @docs/spdd/content-search/01-analysis.md
```

Bu oturum analiz oturumudur; Kanvas, kod ve gözden geçirme burada üretilmez.
