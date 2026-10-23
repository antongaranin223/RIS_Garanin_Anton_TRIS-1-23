<script setup>
import { ref } from 'vue'

// Какой экран показан: 'list' | 'new' | 'login'. Адреса (vue-router) появятся в ЛР4.
const screen = ref('list')

// Коды статусов — как в API и БД (docs/api-contract.md). Подписи — только для экрана.
const statusLabels = {
  New: 'Новая',
  InProgress: 'В ремонте',
  Closed: 'Выполнена',
  Cancelled: 'Отменена',
}

// Тестовые заявки на ремонт. Поля совпадают с ответом GET /api/work-orders из ЛР2.
const workOrders = ref([
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

// Поля формы создания — тело POST /api/work-orders: title, equipmentId, description.
const newWorkOrder = ref({ title: '', equipmentId: null, description: '' })

// Поля формы входа — тело POST /api/auth/login.
const credentials = ref({ login: '', password: '' })

// Сообщение-заглушка: сервера пока нет, ничего не сохраняется.
const notice = ref('')

function showScreen(name) {
  screen.value = name
  notice.value = ''
}

function submitWorkOrder() {
  notice.value = 'Заявка пока не сохраняется: отправка на сервер появится в следующих работах.'
}

function submitLogin() {
  notice.value = 'Вход пока не проверяется: авторизация появится в следующих работах.'
}

// Ставит фокус на поле, когда экран появился (атрибут autofocus срабатывает только при загрузке страницы).
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <header class="app-header">
    <p class="brand">ТОиР-Цех <span>заявки на ремонт оборудования</span></p>
    <nav aria-label="Экраны">
      <button
        type="button"
        :aria-pressed="screen === 'list'"
        @click="showScreen('list')"
      >
        Список
      </button>
      <button
        type="button"
        :aria-pressed="screen === 'new'"
        @click="showScreen('new')"
      >
        Создать
      </button>
      <button
        type="button"
        :aria-pressed="screen === 'login'"
        @click="showScreen('login')"
      >
        Вход
      </button>
    </nav>
  </header>

  <main v-if="screen === 'list'">
    <h1>Заявки на ремонт</h1>
    <table>
      <caption>
        Тестовые данные: сервер подключается в следующих работах
      </caption>
      <thead>
        <tr>
          <th scope="col">Номер</th>
          <th scope="col">Заголовок</th>
          <th scope="col">Оборудование (equipmentId)</th>
          <th scope="col">Исполнитель</th>
          <th scope="col">Статус</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in workOrders" :key="item.id">
          <td class="nowrap">{{ item.number }}</td>
          <td>{{ item.title }}</td>
          <td>{{ item.equipmentId }}</td>
          <td>{{ item.assignee ? item.assignee.fullName : '—' }}</td>
          <td>
            <span class="status" :class="`status-${item.status}`" :title="item.status">
              {{ statusLabels[item.status] }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </main>

  <main v-else-if="screen === 'new'">
    <h1>Новая заявка на ремонт</h1>
    <form class="card" @submit.prevent="submitWorkOrder">
      <label for="title">Заголовок — краткая суть неисправности, 5–80 символов</label>
      <input
        id="title"
        v-model="newWorkOrder.title"
        v-focus
        name="title"
        type="text"
        autofocus
        placeholder="Не включается шпиндель"
      />

      <label for="equipmentId">Номер оборудования (equipmentId), целое число больше 0</label>
      <input
        id="equipmentId"
        v-model.number="newWorkOrder.equipmentId"
        name="equipmentId"
        type="number"
        min="1"
        step="1"
        placeholder="3"
      />

      <label for="description">Описание — симптомы и обстоятельства, 10–500 символов</label>
      <textarea
        id="description"
        v-model="newWorkOrder.description"
        name="description"
        rows="5"
        placeholder="После смены инструмента шпиндель не стартует, на пульте ошибка E-12. Станок остановлен."
      ></textarea>

      <div class="actions">
        <button type="submit" class="primary">Создать</button>
        <button type="button" @click="showScreen('list')">Отмена</button>
      </div>
    </form>
    <p v-if="notice" class="notice" role="status">{{ notice }}</p>
  </main>

  <main v-else>
    <h1>Вход в систему</h1>
    <form class="card narrow" @submit.prevent="submitLogin">
      <label for="login">Логин</label>
      <input
        id="login"
        v-model="credentials.login"
        v-focus
        name="login"
        type="text"
        autocomplete="username"
      />

      <label for="password">Пароль</label>
      <input
        id="password"
        v-model="credentials.password"
        name="password"
        type="password"
        autocomplete="current-password"
      />

      <div class="actions">
        <button type="submit" class="primary">Войти</button>
      </div>
    </form>
    <p v-if="notice" class="notice" role="status">{{ notice }}</p>
  </main>
</template>

<style scoped>
.app-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 24px;
  background: var(--header-bg);
  color: var(--header-text);
}

.brand {
  margin: 0;
  font-weight: 700;
  font-size: 1.1rem;
}

.brand span {
  margin-left: 8px;
  font-weight: 400;
  font-size: 0.9rem;
  opacity: 0.8;
}

nav {
  display: flex;
  gap: 8px;
}

nav button {
  background: transparent;
  color: var(--header-text);
  border-color: rgb(255 255 255 / 0.4);
}

nav button[aria-pressed='true'] {
  background: var(--header-text);
  color: var(--header-bg);
  border-color: var(--header-text);
}

main {
  max-width: 1100px;
  margin: 0 auto;
  padding: 24px;
}

h1 {
  margin: 0 0 16px;
  font-size: 1.5rem;
}

table {
  width: 100%;
  border-collapse: collapse;
  background: var(--surface);
}

caption {
  caption-side: bottom;
  padding-top: 8px;
  text-align: left;
  font-size: 0.85rem;
  color: var(--muted);
}

th,
td {
  padding: 10px 12px;
  border-bottom: 1px solid var(--border);
  text-align: left;
  vertical-align: top;
}

th {
  background: var(--surface-alt);
  font-weight: 600;
}

.nowrap {
  white-space: nowrap;
}

.status {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 0.85rem;
  white-space: nowrap;
}

.status-New {
  background: #dbeafe;
  color: #1e3a8a;
}

.status-InProgress {
  background: #fef3c7;
  color: #78350f;
}

.status-Closed {
  background: #dcfce7;
  color: #14532d;
}

.status-Cancelled {
  background: #e5e7eb;
  color: #374151;
}

.card {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-width: 640px;
  padding: 20px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 8px;
}

.card.narrow {
  max-width: 360px;
}

label {
  margin-top: 10px;
  font-weight: 600;
}

label:first-child {
  margin-top: 0;
}

input,
textarea {
  padding: 8px 10px;
  font: inherit;
  border: 1px solid var(--border-strong);
  border-radius: 6px;
  background: var(--surface);
  color: var(--text);
}

textarea {
  resize: vertical;
}

.actions {
  display: flex;
  gap: 8px;
  margin-top: 16px;
}

.notice {
  max-width: 640px;
  padding: 10px 14px;
  border-left: 4px solid var(--accent);
  background: var(--surface-alt);
}
</style>
