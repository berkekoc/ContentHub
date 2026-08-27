import { generateDataset, paginate } from '../lib/dataset.js';
import { isRateLimited } from '../lib/ratelimit.js';
import { readQuery, send, tooManyRequests, escapeXml } from '../lib/http.js';

const DATASET = generateDataset('xml', 300);

// XML sağlayıcı sözleşmesi: bilinçle zorlaştırılmış — nitelikler, iç içe <stats>,
// farklı alan adları ve KARIŞIK tarih biçimleri (video: dd.MM.yyyy, metin: yyyy-MM-dd).
function formatDate(iso, type) {
  const d = new Date(iso);
  const dd = String(d.getUTCDate()).padStart(2, '0');
  const mm = String(d.getUTCMonth() + 1).padStart(2, '0');
  const yyyy = d.getUTCFullYear();
  return type === 'video' ? `${dd}.${mm}.${yyyy}` : `${yyyy}-${mm}-${dd}`;
}

export default function handler(req, res) {
  const { url, page, pageSize } = readQuery(req);
  if (isRateLimited(url, 'xml', 120)) {
    tooManyRequests(res);
    return;
  }

  const pageData = paginate(DATASET, page, pageSize);
  const parts = [];
  parts.push('<?xml version="1.0" encoding="UTF-8"?>');
  parts.push(`<contents page="${pageData.page}" size="${pageData.pageSize}" total="${pageData.total}">`);
  for (const item of pageData.items) {
    const kind = item.type === 'video' ? 'Video' : 'Text';
    parts.push(`  <content externalId="${escapeXml(item.key)}" kind="${kind}">`);
    parts.push(`    <heading>${escapeXml(item.title)}</heading>`);
    parts.push(`    <summary>${escapeXml(item.description)}</summary>`);
    parts.push(`    <released>${formatDate(item.publishedAt, item.type)}</released>`);
    parts.push(`    <link>${escapeXml(item.url)}</link>`);
    parts.push('    <stats>');
    if (item.type === 'video') {
      parts.push(`      <viewCount>${item.video?.views ?? ''}</viewCount>`);
      parts.push(`      <likeCount>${item.video?.likes ?? ''}</likeCount>`);
    } else {
      parts.push(`      <minutes>${item.text?.readingTime ?? ''}</minutes>`);
      parts.push(`      <reactionCount>${item.text?.reactions ?? ''}</reactionCount>`);
    }
    parts.push('    </stats>');
    parts.push('  </content>');
  }
  parts.push('</contents>');

  send(res, 200, 'application/xml; charset=utf-8', parts.join('\n'));
}
