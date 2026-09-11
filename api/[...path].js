export default async function handler(request, response) {
  const backendUrl = process.env.BACKEND_URL;
  if (!backendUrl) return response.status(500).json({ error: 'BACKEND_URL no está configurada en Vercel.' });

  const path = Array.isArray(request.query.path) ? request.query.path.join('/') : request.query.path || '';
  const target = new URL(`/api/${path}`, backendUrl.endsWith('/') ? backendUrl : `${backendUrl}/`);
  for (const [key, value] of Object.entries(request.query)) {
    if (key !== 'path' && typeof value === 'string') target.searchParams.set(key, value);
  }

  const headers = new Headers();
  for (const [key, value] of Object.entries(request.headers)) {
    if (value && !['host', 'connection', 'content-length'].includes(key.toLowerCase())) headers.set(key, value);
  }

  const body = typeof request.body === 'string' ? request.body : request.body ? JSON.stringify(request.body) : undefined;
  const upstream = await fetch(target, {
    method: request.method,
    headers,
    body: ['GET', 'HEAD'].includes(request.method) ? undefined : body
  });
  const contentType = upstream.headers.get('content-type');
  if (contentType) response.setHeader('content-type', contentType);
  response.status(upstream.status).send(Buffer.from(await upstream.arrayBuffer()));
}
