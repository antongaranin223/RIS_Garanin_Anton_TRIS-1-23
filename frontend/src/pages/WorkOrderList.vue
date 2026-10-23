<script setup>
import { onMounted, ref } from 'vue'
import WorkOrderTable from '../components/WorkOrderTable.vue'
import { workOrders } from '../workOrdersStore.js'
import { errorMessage, getWorkOrders } from '../api/workOrders.js'

// Одно состояние экрана вместо трёх флагов: 'loading' | 'empty' | 'data' | 'error'.
const screen = ref('loading')
const errorText = ref('')

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

onMounted(load)
</script>

<template>
  <main>
    <h1>Заявки на ремонт</h1>

    <p v-if="screen === 'loading'" class="state" role="status" aria-live="polite">Загрузка…</p>

    <div v-else-if="screen === 'empty'" class="state">
      <p>Заявок пока нет.</p>
      <router-link to="/new">Создать первую заявку</router-link>
    </div>

    <WorkOrderTable v-else-if="screen === 'data'" :items="workOrders" />

    <div v-else class="state state-error" role="alert">
      <p>{{ errorText }}</p>
      <button type="button" @click="load">Повторить</button>
    </div>
  </main>
</template>

<style scoped>
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
