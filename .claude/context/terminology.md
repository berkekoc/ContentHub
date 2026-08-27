# Ortak Dil (Ubiquitous Language) — ContentHub

| Terim (TR) | Terim (EN / kod) | Anlam |
|---|---|---|
| İçerik Sağlayıcı | Provider | Dış içerik kaynağı; formatı (JSON/XML), adresi, istek limiti vardır. |
| İçerik | ContentItem | Sağlayıcı formatından bağımsız kanonik içerik nesnesi. |
| İçerik Türü | ContentType (Video \| Text) | Puanlama formülünün dalını ve geçerli ölçüt setini belirler. |
| Etkileşim Ölçütleri | ContentMetrics | Ham sayaçlar. Video: views, likes. Metin: readingTime, reactions. |
| Puan | ContentScore | Formül çıktısı + ara bileşenler (temel, katsayı, güncellik, etkileşim, nihai). |
| Temel Puan | BaseScore | Türe göre ham ölçütlerden hesaplanan taban değer. |
| Tür Katsayısı | TypeCoefficient | Video 1.5, Metin 1.0. |
| Güncellik Puanı | RecencyScore | Yayın tarihine göre +5 / +3 / +1 / +0. Zamana bağlıdır. |
| Etkileşim Puanı | EngagementScore | Video: (likes/views)*10, Metin: (reactions/readingTime)*5. |
| Nihai Skor | FinalScore | (Temel × Katsayı) + Güncellik + Etkileşim. |
| Popülerlik | Popularity | Arama teriminden bağımsız, içeriğin kendi nihai skoru. |
| Alakalılık | Relevance | Arama terimine göre metin eşleşme kuvveti (PostgreSQL ts_rank). |
| Çekim Çalıştırması | ProviderFetchRun | Bir sağlayıcıdan veri çekme işinin denetim kaydı. |
| Sağlayıcı Kimliği | ExternalId | İçeriğin kaynak sistemdeki kimliği. (ProviderId, ExternalId) doğal anahtardır. |
| İstek Limiti Politikası | RateLimitPolicy | Sağlayıcı başına birim zamandaki izinli istek sayısı ve aşım davranışı. |
| Sağlayıcı Adaptörü | ProviderAdapter | Sağlayıcı formatını kanonik modele çeviren tek sorumlu bileşen (ACL). |
