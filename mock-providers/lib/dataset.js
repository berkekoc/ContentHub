// Deterministik seed üreteci. İki sağlayıcı (json/xml) çoğunlukla farklı içerik üretir,
// ama ORTAK ("syndicated") bir küme İKİSİNDE de aynı başlık+tür+yayın tarihiyle bulunur →
// içerik parmak izi eşleşir → tekilleştirme kanıtı. Ayrıca bilinçli BOZUK kayıtlar (views=0,
// eksik ölçüt) uç durum dayanıklılığını gösterir.
//
// Tarihler sabit bir epoch'a (BASE_DATE) göre üretilir ki parmak izi zamandan bağımsız
// deterministik kalsın; güncellik puanı yine gerçek "şimdi"ye göre okuma anında hesaplanır.

const BASE_DATE = Date.UTC(2026, 7, 20); // 2026-08-20 (ay 0-indeksli)
const DAY = 24 * 60 * 60 * 1000;

const TITLE_WORDS = [
  'yapay', 'zeka', 'bulut', 'mimari', 'veri', 'bilim', 'arama', 'motor',
  'güvenlik', 'performans', 'ölçek', 'dağıtık', 'sistem', 'tasarım', 'model',
  'algoritma', 'grafik', 'ağ', 'servis', 'platform', 'içerik', 'analiz',
];

function mulberry32(seed) {
  return function next() {
    seed |= 0;
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function words(rng, count) {
  const out = [];
  for (let i = 0; i < count; i++) {
    out.push(TITLE_WORDS[Math.floor(rng() * TITLE_WORDS.length)]);
  }
  return out.join(' ');
}

function titleCase(s) {
  return s.replace(/\b\w/g, (c) => c.toUpperCase());
}

// Ortak (her iki sağlayıcıda bulunan) içerikler — parmak izi eşleşir.
function syndicated() {
  const rng = mulberry32(999);
  const items = [];
  for (let i = 0; i < 24; i++) {
    const type = i % 2 === 0 ? 'video' : 'text';
    const publishedAt = new Date(BASE_DATE - i * 3 * DAY).toISOString();
    items.push({
      key: `syn-${i}`,
      title: titleCase(`${words(rng, 3)} rehberi ${i}`),
      description: words(rng, 12),
      type,
      publishedAt,
      url: `https://cdn.example/syndicated/${i}`,
      video: type === 'video' ? { views: 1000 + i * 50, likes: 40 + i } : null,
      text: type === 'text' ? { readingTime: 3 + (i % 8), reactions: 10 + i } : null,
    });
  }
  return items;
}

// Sağlayıcıya özel içerikler + bilinçli bozuk kayıtlar.
function providerSpecific(providerKey, count) {
  const seed = providerKey === 'json' ? 1001 : 2002;
  const rng = mulberry32(seed);
  const items = [];
  for (let i = 0; i < count; i++) {
    const type = rng() < 0.55 ? 'video' : 'text';
    const dayOffset = Math.floor(rng() * 200); // 0..200 gün geriye
    const publishedAt = new Date(BASE_DATE - dayOffset * DAY).toISOString();
    const broken = i % 60 === 0; // her 60 kayıtta bir bozuk
    items.push({
      key: `${providerKey}-${i}`,
      title: titleCase(`${words(rng, 3 + Math.floor(rng() * 3))} ${providerKey} ${i}`),
      description: words(rng, 10 + Math.floor(rng() * 20)),
      type,
      publishedAt,
      url: `https://cdn.example/${providerKey}/${i}`,
      video:
        type === 'video'
          ? broken
            ? { views: 0, likes: 5 } // sıfıra bölme uç durumu
            : { views: 500 + Math.floor(rng() * 50000), likes: Math.floor(rng() * 2000) }
          : null,
      text:
        type === 'text'
          ? broken
            ? { readingTime: null, reactions: 7 } // eksik ölçüt
            : { readingTime: 1 + Math.floor(rng() * 20), reactions: Math.floor(rng() * 500) }
          : null,
    });
  }
  return items;
}

export function generateDataset(providerKey, count = 300) {
  return [...syndicated(), ...providerSpecific(providerKey, count)];
}

export function paginate(items, page, pageSize) {
  const start = (page - 1) * pageSize;
  return {
    page,
    pageSize,
    total: items.length,
    items: items.slice(start, start + pageSize),
  };
}
