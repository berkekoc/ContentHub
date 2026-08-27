export function readQuery(req) {
  const url = new URL(req.url, 'http://localhost');
  const page = Math.max(1, parseInt(url.searchParams.get('page') ?? '1', 10) || 1);
  const pageSize = Math.min(500, Math.max(1, parseInt(url.searchParams.get('pageSize') ?? '100', 10) || 100));
  return { url, page, pageSize };
}

export function send(res, status, contentType, body) {
  res.statusCode = status;
  res.setHeader('Content-Type', contentType);
  res.end(body);
}

export function tooManyRequests(res) {
  res.statusCode = 429;
  res.setHeader('Retry-After', '5');
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.end(JSON.stringify({ error: 'rate_limited', message: 'Sağlayıcı istek limiti aşıldı.' }));
}

export function escapeXml(value) {
  if (value === null || value === undefined) return '';
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&apos;');
}
