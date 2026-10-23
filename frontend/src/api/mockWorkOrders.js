// Mock — учебная замена ASP.NET Core Web API, пока сервера на C# нет.
// Повторяет контракт docs/api-contract.md: GET /api/work-orders → 200, POST /api/work-orders → 201 / 400.
// Каждая функция возвращает { ok, status, data } — то же, что сервер отдал бы по HTTP.
//
// Страницы этот файл НЕ импортируют. Его подключает dev-сервер Vite (vite.config.js),
// поэтому запросы к mock — настоящие HTTP-запросы, их видно в F12 → Network.
// Данные живут в памяти процесса npm run dev: после F5 сохраняются, после перезапуска Vite — сбрасываются.

const DELAY_MS = 500

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

// Справочник оборудования: какие equipmentId «существуют на сервере» (ФТ-3).
const equipmentIds = new Set([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12])

const workOrders = [
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
]

let nextId = 4

// Номер ЗР-ГГГГ-NNNN ставит «сервер», а не страница.
function makeNumber(id) {
  return `ЗР-${new Date().getFullYear()}-${String(id).padStart(4, '0')}`
}

const serverError = () => ({
  ok: false,
  status: 500,
  data: { status: 500, title: 'Внутренняя ошибка сервера (учебный флаг force500)' },
})

// Те же правила, что на клиенте, плюс проверка, что оборудование есть в справочнике.
function validate(body) {
  const errors = {}
  const title = typeof body.title === 'string' ? body.title.trim() : ''
  if (title.length < 5 || title.length > 80) {
    errors.title = ['Заголовок должен содержать от 5 до 80 символов']
  }
  if (!Number.isInteger(body.equipmentId) || body.equipmentId <= 0) {
    errors.equipmentId = ['Номер оборудования — целое число больше 0']
  } else if (!equipmentIds.has(body.equipmentId)) {
    errors.equipmentId = ['Оборудование с таким id не найдено']
  }
  const description = typeof body.description === 'string' ? body.description.trim() : ''
  if (description.length < 10 || description.length > 500) {
    errors.description = ['Описание должно содержать от 10 до 500 символов']
  }
  return errors
}

// GET /api/work-orders
export async function mockList({ force500 = false, empty = false } = {}) {
  await delay(DELAY_MS)
  if (force500) return serverError()
  // Новые заявки — первыми; пустой список — это 200 и [], а не 404.
  const data = empty ? [] : [...workOrders].reverse()
  return { ok: true, status: 200, data }
}

// POST /api/work-orders
export async function mockCreate(body, { force500 = false } = {}) {
  await delay(DELAY_MS)
  if (force500) return serverError()

  const errors = validate(body ?? {})
  if (Object.keys(errors).length > 0) {
    return { ok: false, status: 400, data: { status: 400, title: 'Ошибка проверки полей', errors } }
  }

  // id, number, status, assignee определяет сервер; клиент присылает только три поля.
  const created = {
    id: nextId,
    number: makeNumber(nextId),
    title: body.title.trim(),
    equipmentId: body.equipmentId,
    description: body.description.trim(),
    status: 'New',
    assignee: null,
  }
  nextId += 1
  workOrders.push(created)
  return { ok: true, status: 201, data: created }
}
