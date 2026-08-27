// Yerel standalone sunucu (docker-compose / yerel geliştirme). Vercel'de her dosya
// api/ altında ayrı serverless uç olur; burada ikisini tek http sunucusunda birleştiriyoruz.
import http from 'node:http';
import jsonHandler from './api/json.js';
import xmlHandler from './api/xml.js';

const PORT = process.env.PORT ?? 4010;

const server = http.createServer((req, res) => {
  const path = new URL(req.url, 'http://localhost').pathname;
  if (path === '/api/json') {
    jsonHandler(req, res);
  } else if (path === '/api/xml') {
    xmlHandler(req, res);
  } else if (path === '/health') {
    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/json');
    res.end(JSON.stringify({ status: 'ok' }));
  } else {
    res.statusCode = 404;
    res.end('not found');
  }
});

server.listen(PORT, () => {
  // eslint-disable-next-line no-console
  console.log(`Mock providers listening on http://localhost:${PORT} (/api/json, /api/xml)`);
});
