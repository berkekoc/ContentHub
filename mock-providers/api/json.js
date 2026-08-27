import { generateDataset, paginate } from '../lib/dataset.js';
import { isRateLimited } from '../lib/ratelimit.js';
import { readQuery, send, tooManyRequests } from '../lib/http.js';

const DATASET = generateDataset('json', 300);

// JSON sağlayıcı sözleşmesi: JsonProviderAdapter ile birebir uyumlu.
export default function handler(req, res) {
  const { url, page, pageSize } = readQuery(req);
  if (isRateLimited(url, 'json', 120)) {
    tooManyRequests(res);
    return;
  }

  const pageData = paginate(DATASET, page, pageSize);
  const payload = {
    page: pageData.page,
    pageSize: pageData.pageSize,
    total: pageData.total,
    items: pageData.items.map((item) => ({
      id: item.key,
      title: item.title,
      description: item.description,
      type: item.type,
      publishedAt: item.publishedAt,
      url: item.url,
      metrics:
        item.type === 'video'
          ? { views: item.video?.views ?? null, likes: item.video?.likes ?? null }
          : { readingTime: item.text?.readingTime ?? null, reactions: item.text?.reactions ?? null },
    })),
  };

  send(res, 200, 'application/json; charset=utf-8', JSON.stringify(payload));
}
