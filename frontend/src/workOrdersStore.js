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
    status: 'New',
    assignee: null,
  },
  {
    id: 2,
    number: 'ЗР-2026-0002',
    title: 'Течь масла из гидроцилиндра пресса',
    equipmentId: 7,
    status: 'InProgress',
    assignee: { id: 5, fullName: 'Петров С. И.' },
  },
  {
    id: 3,
    number: 'ЗР-2026-0003',
    title: 'Плановое ТО компрессора',
    equipmentId: 12,
    status: 'Closed',
    assignee: { id: 6, fullName: 'Сидоров В. А.' },
  },
])
