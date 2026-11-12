<script setup>
import { computed, onMounted, ref } from 'vue'
import WorkOrderTable from '../components/WorkOrderTable.vue'
import { workOrders } from '../workOrdersStore.js'
import {
  assignWorkOrder,
  changeWorkOrderStatus,
  errorMessage,
  getWorkOrders,
} from '../api/workOrders.js'
import { getEquipment, getFitters } from '../api/reference.js'
import { currentUser } from '../api/session.js'

// Одно состояние экрана вместо трёх флагов: 'loading' | 'empty' | 'data' | 'error'.
const screen = ref('loading')
const errorText = ref('')

// Справочники для подписей и для «Назначить».
const equipmentById = ref({})
const fitters = ref([])

// Действие над строкой: какая заявка сейчас отправляется и какие отказы показать у строк.
const busyId = ref(null)
const rowErrors = ref({})
const notice = ref('')

// Фильтры списка (на клиенте): статус для начальника цеха (ФТ-11), «Мои заявки» для слесаря (ФТ-12).
const statusFilter = ref('')
const onlyMine = ref(false)
const isFitter = computed(() => currentUser.value?.role === 'Fitter')

const visible = computed(() =>
  workOrders.value.filter(
    (w) =>
      (!statusFilter.value || w.status === statusFilter.value) &&
      (!onlyMine.value || w.assigneeUserId === currentUser.value.id),
  ),
)

async function loadReferences() {
  // Справочники не обязательны для списка: если не загрузились, в строке будет «id 3» вместо названия.
  try {
    const equipment = await getEquipment()
    equipmentById.value = Object.fromEntries(equipment.map((e) => [e.id, e]))
  } catch {
    equipmentById.value = {}
  }
  if (currentUser.value?.role === 'Mechanic') {
    try {
      fitters.value = await getFitters()
    } catch {
      fitters.value = []
    }
  }
}

async function load() {
  screen.value = 'loading'
  errorText.value = ''
  try {
    workOrders.value = await getWorkOrders()
    screen.value = workOrders.value.length === 0 ? 'empty' : 'data'
  } catch (error) {
    errorText.value = errorMessage(error)
    screen.value = 'error'
  }
}

// Общий путь для «Назначить», «В ремонт», «Выполнена», «Отменить»:
// запрос → заменить строку ответом сервера (статус, исполнитель, updatedAt) или показать отказ у строки.
async function act(id, request, doneText) {
  if (busyId.value !== null) return
  busyId.value = id
  notice.value = ''
  rowErrors.value = { ...rowErrors.value, [id]: '' }
  try {
    const updated = await request()
    const index = workOrders.value.findIndex((w) => w.id === id)
    if (index !== -1) workOrders.value[index] = updated
    notice.value = `${updated.number}: ${doneText}`
  } catch (error) {
    // 403 — «Нет прав на изменение этой заявки…» (ФТ-8), 409 — недопустимый переход: текст сервера.
    rowErrors.value = { ...rowErrors.value, [id]: errorMessage(error) }
  } finally {
    busyId.value = null
  }
}

const statusDone = {
  InProgress: 'переведена в ремонт',
  Closed: 'выполнена',
  Cancelled: 'отменена',
}

function onAssign(id, fitterId) {
  const name = fitters.value.find((f) => f.id === fitterId)?.fullName ?? `id ${fitterId}`
  act(id, () => assignWorkOrder(id, fitterId), `назначен исполнитель ${name}`)
}

function onStatus(id, status) {
  act(id, () => changeWorkOrderStatus(id, status), statusDone[status])
}

onMounted(() => {
  loadReferences()
  load()
})
</script>

<template>
  <main>
    <h1>Заявки на ремонт</h1>

    <div v-if="screen === 'data'" class="filters">
      <label>
        Статус
        <select v-model="statusFilter">
          <option value="">все</option>
          <option value="New">Новая</option>
          <option value="InProgress">В ремонте</option>
          <option value="Closed">Выполнена</option>
          <option value="Cancelled">Отменена</option>
        </select>
      </label>
      <label v-if="isFitter" class="mine">
        <input v-model="onlyMine" type="checkbox" />
        Мои заявки
      </label>
      <button type="button" @click="load">Обновить</button>
    </div>

    <p v-if="notice" class="notice" role="status">{{ notice }}</p>

    <p v-if="screen === 'loading'" class="state" role="status" aria-live="polite">Загрузка…</p>

    <div v-else-if="screen === 'empty'" class="state">
      <p>Заявок пока нет.</p>
      <router-link v-if="['Foreman', 'Mechanic'].includes(currentUser?.role)" to="/new">
        Создать первую заявку
      </router-link>
    </div>

    <template v-else-if="screen === 'data'">
      <WorkOrderTable
        v-if="visible.length > 0"
        :items="visible"
        :user="currentUser"
        :equipment-by-id="equipmentById"
        :fitters="fitters"
        :busy-id="busyId"
        :row-errors="rowErrors"
        @assign="onAssign"
        @status="onStatus"
      />
      <p v-else class="state">По выбранному фильтру заявок нет.</p>
    </template>

    <div v-else class="state state-error" role="alert">
      <p>{{ errorText }}</p>
      <button type="button" @click="load">Повторить</button>
    </div>
  </main>
</template>

<style scoped>
.filters {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
}

.filters label {
  display: flex;
  align-items: center;
  gap: 8px;
}

.mine input {
  width: auto;
}

.state {
  padding: 20px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 8px;
}

.state p {
  margin: 0 0 8px;
}

.state p:last-child {
  margin-bottom: 0;
}

.state-error {
  border-left: 4px solid var(--danger);
}
</style>
