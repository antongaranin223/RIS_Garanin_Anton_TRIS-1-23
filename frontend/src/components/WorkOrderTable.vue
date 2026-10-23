<script setup>
// Таблица только рисует то, что ей передали через props.
// Своего массива нет, полученный массив не копирует и не меняет.
defineProps({
  items: { type: Array, required: true },
})

// Коды статусов — как в API и БД. Подписи — только для экрана.
const statusLabels = {
  New: 'Новая',
  InProgress: 'В ремонте',
  Closed: 'Выполнена',
  Cancelled: 'Отменена',
}
</script>

<template>
  <table>
    <caption>
      Новые заявки — сверху. Данные получены из API (GET /api/work-orders)
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
      <tr v-for="item in items" :key="item.id">
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
</style>
