// Вход: POST /api/auth/login → { token, expiresAt, user }. Токен запоминает session.js.
import { request } from './http.js'
import { clearSession, saveSession } from './session.js'

export async function login(loginName, password) {
  const result = await request('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ login: loginName, password }),
  })
  saveSession(result)
  return result.user
}

// Выход — просто забыть токен в браузере (JWT без серверной сессии; истечёт сам через 8 часов).
export function logout() {
  clearSession()
}
