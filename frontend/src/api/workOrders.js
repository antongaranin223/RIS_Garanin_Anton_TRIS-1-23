// Клиент API заявок на ремонт: список, создание, назначение исполнителя и смена статуса.
// Адрес, токен и разбор ошибок — в api/http.js.
import { request } from './http.js'

export { ApiError, errorMessage, USE_MOCK } from './http.js'

// GET /api/work-orders → массив заявок (или []). Нужен вход, иначе 401.
export function getWorkOrders() {
  return request('/api/work-orders')
}

// POST /api/work-orders → созданная заявка с id, number и status из ответа сервера.
// body: { title, equipmentId, description } — больше клиент ничего не присылает.
// Автора сервер берёт из токена; слесарю и начальнику цеха сервер ответит 403.
export function createWorkOrder(body) {
  return request('/api/work-orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

// PATCH /api/work-orders/{id}/assignee → заявка с исполнителем, статус остаётся New. Только механик цеха.
export function assignWorkOrder(id, assigneeUserId) {
  return request(`/api/work-orders/${id}/assignee`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ assigneeUserId }),
  })
}

// PATCH /api/work-orders/{id}/status → заявка с новым статусом. Кто может — решает сервер (403 / 409).
export function changeWorkOrderStatus(id, status) {
  return request(`/api/work-orders/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status }),
  })
}
