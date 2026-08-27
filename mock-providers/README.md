# Mock Providers (JSON + XML)

ContentHub için sahte içerik sağlayıcıları. Gerçek sağlayıcı davranışını taklit eder:
sayfalama (`?page=&pageSize=`), istek limitinde `429`, ve **bilinçle farklı biçimler**.

## Uçlar
- `GET /api/json` — JSON sağlayıcı (JsonProviderAdapter sözleşmesi).
- `GET /api/xml` — XML sağlayıcı (XmlProviderAdapter sözleşmesi; nitelikler, iç içe `<stats>`,
  karışık tarih biçimleri: video `dd.MM.yyyy`, metin `yyyy-MM-dd`).
- `?simulate429=1` — istek limiti yolunu zorlar (429 döner).

## Seed
- Sağlayıcı başına ~324 içerik (24 ortak "syndicated" + 300 özel).
- **Ortak küme** her iki sağlayıcıda aynı başlık+tür+tarihle bulunur → parmak izi eşleşir →
  tekilleştirme kanıtı (aynı içerik iki sağlayıcıda → tek temsilci).
- **Bozuk kayıtlar** (video `views=0`, metin `readingTime` eksik) → uç durum dayanıklılığı kanıtı.

## Çalıştırma
```
npm start           # yerel: http://localhost:4010/api/json , /api/xml
```
Vercel'de `api/json.js` ve `api/xml.js` ayrı serverless uçlara dağıtılır.
