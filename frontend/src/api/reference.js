// Справочники для экрана (docs/api-contract.md, §2.2).
import { request } from './http.js'

// GET /api/equipment → [{ id, inventoryNumber, name, areaId, areaName }] — выбор оборудования в форме, подписи в списке.
export function getEquipment() {
  return request('/api/equipment')
}

// GET /api/users?role=Fitter → [{ id, fullName, role }] — кого можно назначить. Только механику цеха (иначе 403).
export function getFitters() {
  return request('/api/users?role=Fitter')
}
