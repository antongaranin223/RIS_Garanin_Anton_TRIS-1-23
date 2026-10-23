import { ref } from 'vue'

// Единственный общий массив заявок на ремонт для всех страниц клиента.
// Поля совпадают с ответом GET /api/work-orders из ЛР2 (docs/api-contract.md).
// Это не серверное хранение: после F5 модуль создаётся заново и массив возвращается к тестовым данным.
export const workOrders = ref([
  {
    id: 1,
    number: 'ЗР-2026-0001',
    title: 'Не включается шпиндель',
    equipmentId: 3,
    description: 'После смены инструмента шпиндель не стартует, на пульте ошибка E-12. Станок остановлен.',
    status: 'New',
    assignee: null,
  },
  {
    id: 2,
    number: 'ЗР-2026-0002',
    title: 'Течь масла из гидроцилиндра пресса',
    equipmentId: 7,
    description: 'Под прессом лужа масла, давление в гидросистеме падает через 10 минут работы.',
    status: 'InProgress',
    assignee: { id: 5, fullName: 'Петров С. И.' },
  },
  {
    id: 3,
    number: 'ЗР-2026-0003',
    title: 'Плановое ТО компрессора',
    equipmentId: 12,
    description: 'Замена масла и фильтров по наработке 2000 часов.',
    status: 'Closed',
    assignee: { id: 6, fullName: 'Сидоров В. А.' },
  },
])

// Учебный счётчик id: следующий после тестовых записей. В ЛР6 id и number будет выдавать сервер.
let nextId = 4

// Добавляет новую заявку в общий массив и возвращает её.
// Поля, которые на сервере ставит не клиент (id, number, status, assignee), задаются здесь.
export function addWorkOrder({ title, equipmentId, description }) {
  const workOrder = {
    id: nextId,
    number: `TMP-${nextId}`,
    title,
    equipmentId,
    description,
    status: 'New',
    assignee: null,
  }
  nextId += 1
  // Новая заявка — первой в списке (UC-1 из ЛР1).
  workOrders.value.unshift(workOrder)
  return workOrder
}
