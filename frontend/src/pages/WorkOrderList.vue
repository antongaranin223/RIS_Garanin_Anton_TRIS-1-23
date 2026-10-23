<script setup>
import WorkOrderTable from '../components/WorkOrderTable.vue'
import { workOrders } from '../workOrdersStore.js'
// Страница берёт общий массив из workOrdersStore.js и передаёт его таблице через props.
</script>

<template>
  <main>
    <h1>Заявки на ремонт</h1>
    <WorkOrderTable :items="workOrders" />
  </main>
</template>
