<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// Поля формы — тело будущего POST /api/work-orders: title, equipmentId, description.
const newWorkOrder = ref({ title: '', equipmentId: null, description: '' })

// Сообщение-заглушка: запись в общий массив и проверка полей — в ЛР5.
const notice = ref('')

function submitWorkOrder() {
  notice.value = 'Заявка пока не сохраняется: запись в список появится в следующей работе.'
}

// Фокус на первое поле при открытии страницы (autofocus срабатывает только при первой загрузке).
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <main>
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
        <button type="button" @click="router.push('/')">Отмена</button>
      </div>
    </form>
    <p v-if="notice" class="notice" role="status">{{ notice }}</p>
  </main>
</template>
