// En basit, örneklem-içi (per-instance) kayan pencere limiti. Gerçek üretim limiti değil;
// sağlayıcı davranışını (429) taklit etmek içindir. `?simulate429=1` ile zorlanabilir.
const hits = new Map();

export function isRateLimited(url, key, limitPerMinute = 120) {
  if (url.searchParams.get('simulate429') === '1') {
    return true;
  }
  const now = Date.now();
  const windowStart = now - 60_000;
  const timestamps = (hits.get(key) ?? []).filter((t) => t > windowStart);
  timestamps.push(now);
  hits.set(key, timestamps);
  return timestamps.length > limitPerMinute;
}
