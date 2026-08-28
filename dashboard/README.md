# ContentHub Dashboard (Next.js + TypeScript)

`GET /api/search`'ü tüketen basit arama arayüzü (Kanvas O12 / R10):
anahtar kelime, tür filtresi, sıralama seçici (Popülerlik/Alakalılık), offset sayfalama;
her sonuç **Başlık · İçerik Türü · Skor**; kopya içerik "N sağlayıcıda mevcut" rozetiyle; bonus
"skor neden?" açıklaması (`/api/content/{id}/score`); açılışta `/health` uyandırma yoklaması.

## Çalıştırma
```bash
npm install
cp .env.local.example .env.local   # NEXT_PUBLIC_API_BASE_URL'i API adresine ayarla
npm run dev                        # http://localhost:3000
```
Üretim: `npm run build && npm start`. Vercel'e doğrudan dağıtılabilir (kök: `dashboard/`).

API tarayıcıdan çağrıldığı için API'de CORS açıktır (demo varsayılanı); üretimde
`ContentHub:Cors:AllowedOrigins` ile dashboard originine kısıtlayın.
