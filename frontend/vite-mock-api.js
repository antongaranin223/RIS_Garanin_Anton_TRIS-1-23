// Плагин Vite: во время npm run dev отвечает на /api/work-orders данными из src/api/mockWorkOrders.js.
// Так mock отвечает настоящими HTTP-кодами (200, 201, 400, 500), а запросы видны в F12 → Network.
// В сборку (npm run build) не попадает: работает только в dev-сервере.
import { mockCreate, mockList } from './src/api/mockWorkOrders.js'

const PATH = '/api/work-orders'

function readJsonBody(req) {
  return new Promise((resolve) => {
    let raw = ''
    req.on('data', (chunk) => {
      raw += chunk
    })
    req.on('end', () => {
      try {
        resolve(raw ? JSON.parse(raw) : {})
      } catch {
        resolve(null) // не JSON — mock ответит 400
      }
    })
  })
}

function send(res, { status, data }, extraHeaders = {}) {
  res.statusCode = status
  res.setHeader('Content-Type', 'application/json; charset=utf-8')
  for (const [name, value] of Object.entries(extraHeaders)) res.setHeader(name, value)
  res.end(JSON.stringify(data))
}

export function mockApiPlugin() {
  return {
    name: 'toir-mock-api',
    apply: 'serve',
    configureServer(server) {
      server.middlewares.use(async (req, res, next) => {
        const url = new URL(req.url, 'http://localhost')
        if (url.pathname !== PATH) return next()

        // Учебные флаги приходят заголовками от src/api/workOrders.js.
        const force500 = req.headers['x-mock-force-500'] === '1'
        const empty = req.headers['x-mock-empty'] === '1'

        if (req.method === 'GET') {
          return send(res, await mockList({ force500, empty }))
        }
        if (req.method === 'POST') {
          const body = await readJsonBody(req)
          const result = await mockCreate(body, { force500 })
          const headers = result.status === 201 ? { Location: `${PATH}/${result.data.id}` } : {}
          return send(res, result, headers)
        }
        // DELETE и прочие методы контрактом не предусмотрены.
        return send(res, { status: 405, data: { status: 405, title: 'Метод не поддерживается' } })
      })
    },
  }
}
