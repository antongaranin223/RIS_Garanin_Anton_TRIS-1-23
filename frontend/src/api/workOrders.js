// Клиент API заявок на ремонт. Страницы знают только две функции: getWorkOrders и createWorkOrder.
// Откуда приходят данные (mock или настоящий сервер) и по какому адресу — решается только здесь.

// Единственный переключатель источника данных.
//   true  — учебный mock (src/api/mockWorkOrders.js) на dev-сервере Vite, тот же origin :5173;
//   false — настоящий ASP.NET Core Web API по адресу из .env (VITE_API_BASE_URL, порт 5000).
export const USE_MOCK = true

const baseUrl = USE_MOCK ? '' : import.meta.env.VITE_API_BASE_URL

// Учебные флаги mock задаются в адресе страницы, чтобы воспроизводить отказы без правки кода:
//   ?force500 — mock отвечает 500 на любой запрос;
//   ?empty    — mock отдаёт пустой список (200 и []).
function mockHeaders() {
  if (!USE_MOCK) return {}
  const params = new URLSearchParams(window.location.search)
  const headers = {}
  if (params.has('force500')) headers['X-Mock-Force-500'] = '1'
  if (params.has('empty')) headers['X-Mock-Empty'] = '1'
  return headers
}

// Ошибка API с понятным видом отказа:
//   'validation' — сервер ответил 400 (поля не прошли проверку), в data.errors — ошибки по полям;
//   'server'     — сервер ответил 5xx;
//   'http'       — другой неуспешный код (401, 403, 404, 409…);
//   'network'    — ответа не было вовсе (сервер не запущен, неверный адрес, нет сети).
export class ApiError extends Error {
  constructor(kind, message, { status = null, data = null } = {}) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.data = data
  }
}

async function request(path, options = {}) {
  let response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...options,
      headers: { Accept: 'application/json', ...mockHeaders(), ...options.headers },
    })
  } catch (cause) {
    // Сюда попадаем только если ответа нет. Это НЕ 500: сервер ничего не ответил.
    throw new ApiError('network', `Нет ответа от ${baseUrl || window.location.origin}${path}`, {
      data: { cause: String(cause) },
    })
  }

  // 400 и 500 — это полученный ответ: fetch не бросает исключение, проверяем response.ok сами.
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    const kind =
      response.status === 400 ? 'validation' : response.status >= 500 ? 'server' : 'http'
    throw new ApiError(kind, data?.title ?? `HTTP ${response.status}`, {
      status: response.status,
      data,
    })
  }
  return data
}

// GET /api/work-orders → массив заявок (или []).
export function getWorkOrders() {
  return request('/api/work-orders')
}

// POST /api/work-orders → созданная заявка с id, number и status из ответа сервера.
// body: { title, equipmentId, description } — больше клиент ничего не присылает.
export function createWorkOrder(body) {
  return request('/api/work-orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

// Текст для человека по виду отказа — одинаковый для списка и формы.
export function errorMessage(error) {
  if (!(error instanceof ApiError)) return 'Непредвиденная ошибка в приложении.'
  switch (error.kind) {
    case 'validation':
      return 'Проверьте поля: сервер не принял данные.'
    case 'server':
      return 'Сервер не смог обработать запрос. Попробуйте позже.'
    case 'network':
      return 'Нет связи с сервером. Проверьте, что API запущен и адрес в .env верный.'
    default:
      return `Запрос отклонён сервером (код ${error.status}).`
  }
}
