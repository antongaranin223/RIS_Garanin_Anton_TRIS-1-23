// Общий HTTP-клиент: адрес API, заголовок Authorization, разбор ответа и вид отказа.
// Им пользуются api/auth.js (вход) и api/workOrders.js (заявки).
import { clearSession, getToken } from './session.js'

// Единственный переключатель источника данных.
//   false — настоящий ASP.NET Core Web API по адресу из .env (VITE_API_BASE_URL, порт 5000). С ЛР11 вход
//           проверяет сервер, поэтому по умолчанию работаем с ним;
//   true  — учебный mock ЛР6 (src/api/mockWorkOrders.js) на dev-сервере Vite. Входа в mock нет.
export const USE_MOCK = false

const baseUrl = USE_MOCK ? '' : (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000')

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
//   'unauthorized' — 401: вход не выполнен, токен истёк или неверный логин/пароль;
//   'forbidden'  — 403: вход выполнен, но этой роли или не владельцу действие не разрешено;
//   'server'     — сервер ответил 5xx;
//   'http'       — другой неуспешный код (404, 409…);
//   'network'    — ответа не было вовсе (сервер не запущен, неверный адрес, CORS).
export class ApiError extends Error {
  constructor(kind, message, { status = null, data = null } = {}) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.data = data
  }
}

function kindOf(status) {
  if (status === 400) return 'validation'
  if (status === 401) return 'unauthorized'
  if (status === 403) return 'forbidden'
  if (status >= 500) return 'server'
  return 'http'
}

// Что сделать, когда сервер ответил 401 на запрос с токеном (истёк). Задаёт router.js: открыть /login.
let onSessionExpired = () => {}
export function setSessionExpiredHandler(handler) {
  onSessionExpired = handler
}

export async function request(path, options = {}) {
  const token = getToken()
  let response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...options,
      headers: {
        Accept: 'application/json',
        // Токен из входа — в каждый запрос. Без него сервер ответит 401.
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...mockHeaders(),
        ...options.headers,
      },
    })
  } catch (cause) {
    // Сюда попадаем только если ответа нет. Это НЕ 500: сервер ничего не ответил (или ответ заблокировал CORS).
    throw new ApiError('network', `Нет ответа от ${baseUrl || window.location.origin}${path}`, {
      data: { cause: String(cause) },
    })
  }

  // 400, 401, 403 и 500 — это полученный ответ: fetch не бросает исключение, проверяем response.ok сами.
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    if (response.status === 401 && token) {
      // Токен был, но сервер его не принял (истёк или испорчен) — сессия больше не действует.
      clearSession()
      onSessionExpired()
    }
    throw new ApiError(kindOf(response.status), data?.title ?? `HTTP ${response.status}`, {
      status: response.status,
      data,
    })
  }
  return data
}

// Текст для человека по виду отказа — одинаковый для списка, формы и входа.
export function errorMessage(error) {
  if (!(error instanceof ApiError)) return 'Непредвиденная ошибка в приложении.'
  switch (error.kind) {
    case 'validation':
      return 'Проверьте поля: сервер не принял данные.'
    case 'unauthorized':
      // У 401 при входе сервер пишет «Неверный логин или пароль» — показываем его текст.
      return error.data?.title ?? 'Войдите в систему.'
    case 'forbidden':
      // ФТ-8: «Нет прав на изменение этой заявки» — текст приходит от сервера.
      return error.data?.title ?? 'Нет прав на это действие.'
    case 'server':
      return 'Сервер не смог обработать запрос. Попробуйте позже.'
    case 'network':
      return 'Нет связи с сервером. Проверьте, что API запущен и адрес в .env верный.'
    default:
      return error.data?.title ?? `Запрос отклонён сервером (код ${error.status}).`
  }
}
