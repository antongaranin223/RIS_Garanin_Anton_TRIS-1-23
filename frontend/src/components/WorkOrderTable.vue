<script setup>
import { reactive } from 'vue'

// Таблица рисует то, что ей передали через props, и сообщает странице о нажатых кнопках (emit).
// Своего массива заявок нет, полученный массив не копирует и не меняет. Запросы к API делает страница.
const props = defineProps({
  items: { type: Array, required: true },
  // Текущий пользователь { id, role } — чтобы показать только подходящие кнопки.
  user: { type: Object, required: true },
  // Оборудование по id → { inventoryNumber, name, areaName } для подписи вместо голого equipmentId.
  equipmentById: { type: Object, default: () => ({}) },
  // Слесари для «Назначить» (загружаются только механику цеха).
  fitters: { type: Array, default: () => [] },
  // id заявки, по которой идёт запрос: её кнопки заблокированы.
  busyId: { type: Number, default: null },
  // Текст отказа по строке { [id]: 'Нет прав…' } — показываем рядом с заявкой.
  rowErrors: { type: Object, default: () => ({}) },
})

const emit = defineEmits(['assign', 'status'])

// Выбранный в выпадающем списке слесарь для каждой строки — только состояние экрана.
const chosenFitter = reactive({})

// Пользовательский текст (заголовок, описание) выводим только интерполяцией {{ }}: Vue экранирует
// < > & ", и описание с тегом script видно как текст, а не выполняется. v-html здесь запрещён.

// Коды статусов — как в API и БД. Подписи — только для экрана.
const statusLabels = {
  New: 'Новая',
  InProgress: 'В ремонте',
  Closed: 'Выполнена',
  Cancelled: 'Отменена',
}

// Какие кнопки показать. Это только удобство: запрещает сервер (403/409), даже если кнопку нажать в обход.
function canAssign(w) {
  return props.user.role === 'Mechanic' && w.status === 'New'
}
function canStart(w) {
  return props.user.role === 'Fitter' && w.status === 'New' && w.assigneeUserId === props.user.id
}
function canClose(w) {
  return props.user.role === 'Fitter' && w.status === 'InProgress' && w.assigneeUserId === props.user.id
}
function canCancel(w) {
  if (w.status !== 'New') return false
  return props.user.role === 'Mechanic' || (props.user.role === 'Foreman' && w.createdByUserId === props.user.id)
}

function equipmentLabel(id) {
  const e = props.equipmentById[id]
  return e ? `${e.inventoryNumber} · ${e.name}` : `id ${id}`
}

function onAssign(w) {
  const fitterId = chosenFitter[w.id] ?? w.assigneeUserId
  if (fitterId) emit('assign', w.id, fitterId)
}
</script>

<template>
  <table>
    <caption>
      Новые заявки — сверху. Данные получены из API (GET /api/work-orders); id — машинный ключ строки в
      таблице work_orders
    </caption>
    <thead>
      <tr>
        <th scope="col">Номер / id</th>
        <th scope="col">Заголовок и описание</th>
        <th scope="col">Оборудование</th>
        <th scope="col">Исполнитель</th>
        <th scope="col">Статус</th>
        <th scope="col">Действия</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="item in items" :key="item.id" :data-id="item.id">
        <td class="nowrap">
          {{ item.number }}
          <span class="id">id {{ item.id }}</span>
        </td>
        <td>
          <span class="title">{{ item.title }}</span>
          <p class="description">{{ item.description }}</p>
        </td>
        <td>
          {{ equipmentLabel(item.equipmentId) }}
          <span v-if="equipmentById[item.equipmentId]" class="id">
            {{ equipmentById[item.equipmentId].areaName }}
          </span>
        </td>
        <td>{{ item.assignee ? item.assignee.fullName : '—' }}</td>
        <td>
          <span class="status" :class="`status-${item.status}`" :title="item.status">
            {{ statusLabels[item.status] }}
          </span>
        </td>
        <td class="actions-cell">
          <div v-if="canAssign(item)" class="assign">
            <label class="visually-hidden" :for="`fitter-${item.id}`">Исполнитель для {{ item.number }}</label>
            <select
              :id="`fitter-${item.id}`"
              :value="chosenFitter[item.id] ?? item.assigneeUserId ?? ''"
              :disabled="busyId === item.id"
              @change="chosenFitter[item.id] = Number($event.target.value)"
            >
              <option value="" disabled>Слесарь…</option>
              <option v-for="f in fitters" :key="f.id" :value="f.id">{{ f.fullName }}</option>
            </select>
            <button
              type="button"
              :disabled="busyId === item.id || !(chosenFitter[item.id] ?? item.assigneeUserId)"
              @click="onAssign(item)"
            >
              Назначить
            </button>
          </div>
          <button
            v-if="canStart(item)"
            type="button"
            class="primary"
            :disabled="busyId === item.id"
            @click="emit('status', item.id, 'InProgress')"
          >
            В ремонт
          </button>
          <button
            v-if="canClose(item)"
            type="button"
            class="primary"
            :disabled="busyId === item.id"
            @click="emit('status', item.id, 'Closed')"
          >
            Выполнена
          </button>
          <button
            v-if="canCancel(item)"
            type="button"
            :disabled="busyId === item.id"
            @click="emit('status', item.id, 'Cancelled')"
          >
            Отменить
          </button>
          <p v-if="rowErrors[item.id]" class="row-error" role="alert">{{ rowErrors[item.id] }}</p>
        </td>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
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

.title {
  font-weight: 600;
}

.description {
  margin: 4px 0 0;
  font-size: 0.9rem;
  color: var(--muted);
  white-space: pre-line;
  overflow-wrap: anywhere;
}

.id {
  display: block;
  margin-top: 2px;
  font-size: 0.8rem;
  color: var(--muted);
}

.nowrap {
  white-space: nowrap;
}

.actions-cell {
  min-width: 180px;
}

.actions-cell button {
  margin: 0 6px 6px 0;
}

.assign {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 6px;
}

.assign select {
  max-width: 160px;
}

.row-error {
  margin: 4px 0 0;
  font-size: 0.85rem;
  color: var(--danger);
}

button:disabled,
select:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
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
</style>
