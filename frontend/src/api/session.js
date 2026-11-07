import { computed, ref } from 'vue'

// Сессия клиента: токен и текущий пользователь после входа.
// Хранится в sessionStorage: переживает F5, но пропадает при закрытии вкладки.
// Это учебный компромисс: токен в хранилище браузера доступен скрипту страницы, поэтому главная
// защита — не допускать XSS (пользовательский текст выводим только через {{ }}, без v-html).
const STORAGE_KEY = 'toir.session'

function load() {
  try {
    const saved = JSON.parse(sessionStorage.getItem(STORAGE_KEY))
    // Истёкший токен не восстанавливаем: сервер всё равно ответит 401.
    if (saved?.token && new Date(saved.expiresAt) > new Date()) return saved
  } catch {
    // повреждённая запись — считаем, что входа нет
  }
  return null
}

const session = ref(load())

// Текущий пользователь { id, login, fullName, role } или null — для шапки и проверки маршрутов.
export const currentUser = computed(() => session.value?.user ?? null)

export function getToken() {
  return session.value?.token ?? null
}

export function saveSession({ token, expiresAt, user }) {
  session.value = { token, expiresAt, user }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session.value))
}

export function clearSession() {
  session.value = null
  sessionStorage.removeItem(STORAGE_KEY)
}

// Подписи ролей для экрана; коды — как в API и БД.
export const roleLabels = {
  Foreman: 'мастер участка',
  Mechanic: 'механик цеха',
  Fitter: 'слесарь-ремонтник',
  ShopChief: 'начальник цеха',
}

// Кто создаёт заявки (ФТ-2). Только чтобы спрятать лишнюю кнопку: запрещает сервер (403).
export const canCreate = computed(() => ['Foreman', 'Mechanic'].includes(currentUser.value?.role))
