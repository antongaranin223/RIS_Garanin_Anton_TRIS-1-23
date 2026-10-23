<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { addWorkOrder } from '../workOrdersStore.js'
import { validateWorkOrder } from '../workOrderValidation.js'

const router = useRouter()

// Поля формы — тело будущего POST /api/work-orders: title, equipmentId, description.
function emptyForm() {
  return { title: '', equipmentId: null, description: '' }
}
const form = ref(emptyForm())

// Ошибки по полям: { title?, equipmentId?, description? }
const errors = ref({})

// Защита от двойной отправки: второй Enter до перехода на список ничего не добавит.
let submitting = false

function onCreate() {
  if (submitting) return

  const found = validateWorkOrder(form.value)
  errors.value = found
  if (Object.keys(found).length > 0) {
    return // есть ошибки — в список ничего не добавляем
  }

  submitting = true
  addWorkOrder({
    title: form.value.title.trim(),
    equipmentId: form.value.equipmentId,
    description: form.value.description.trim(),
  })

  errors.value = {}
  form.value = emptyForm()
  router.push('/')
}

// Фокус на первое поле при открытии страницы (autofocus срабатывает только при первой загрузке).
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <main>
    <h1>Новая заявка на ремонт</h1>
    <form class="card" novalidate @submit.prevent="onCreate">
      <label for="title">Заголовок — краткая суть неисправности, 5–80 символов</label>
      <input
        id="title"
        v-model="form.title"
        v-focus
        name="title"
        type="text"
        autofocus
        placeholder="Не включается шпиндель"
        :aria-invalid="Boolean(errors.title)"
        aria-describedby="title-error"
      />
      <p id="title-error" class="field-error" role="alert">{{ errors.title }}</p>

      <label for="equipmentId">Номер оборудования (equipmentId), целое число больше 0</label>
      <input
        id="equipmentId"
        v-model.number="form.equipmentId"
        name="equipmentId"
        type="number"
        min="1"
        step="1"
        placeholder="3"
        :aria-invalid="Boolean(errors.equipmentId)"
        aria-describedby="equipmentId-error"
      />
      <p id="equipmentId-error" class="field-error" role="alert">{{ errors.equipmentId }}</p>

      <label for="description">Описание — симптомы и обстоятельства, 10–500 символов</label>
      <textarea
        id="description"
        v-model="form.description"
        name="description"
        rows="5"
        placeholder="После смены инструмента шпиндель не стартует, на пульте ошибка E-12. Станок остановлен."
        :aria-invalid="Boolean(errors.description)"
        aria-describedby="description-error"
      ></textarea>
      <p id="description-error" class="field-error" role="alert">{{ errors.description }}</p>

      <div class="actions">
        <button type="submit" class="primary">Создать</button>
        <button type="button" @click="router.push('/')">Отмена</button>
      </div>
    </form>
  </main>
</template>

<style scoped>
.field-error {
  min-height: 1.25rem;
  margin: 0;
  font-size: 0.9rem;
  color: var(--danger);
}

input[aria-invalid='true'],
textarea[aria-invalid='true'] {
  border-color: var(--danger);
}
</style>
