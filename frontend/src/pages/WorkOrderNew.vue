<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { workOrders } from '../workOrdersStore.js'
import { validateWorkOrder } from '../workOrderValidation.js'
import { createWorkOrder, errorMessage } from '../api/workOrders.js'

const router = useRouter()

// Поля формы — тело POST /api/work-orders: title, equipmentId, description.
function emptyForm() {
  return { title: '', equipmentId: null, description: '' }
}
const form = ref(emptyForm())

// Ошибки по полям: { title?, equipmentId?, description? } — от клиентской проверки или из ответа 400.
const errors = ref({})
// Общее сообщение об отказе: разный текст для 400, 500 и «нет связи».
const formError = ref('')
// Пока идёт POST, кнопка заблокирована и второй submit ничего не отправит.
const isSubmitting = ref(false)

// Учебный флаг ?skipClientCheck: не проверять поля в браузере, чтобы увидеть ответ 400 от сервера (mock).
const skipClientCheck = new URLSearchParams(window.location.search).has('skipClientCheck')

async function onCreate() {
  if (isSubmitting.value) return

  formError.value = ''
  errors.value = skipClientCheck ? {} : validateWorkOrder(form.value)
  if (Object.keys(errors.value).length > 0) {
    return // клиентская проверка не прошла — запрос не отправляем
  }

  isSubmitting.value = true
  try {
    const created = await createWorkOrder({
      title: form.value.title.trim(),
      equipmentId: form.value.equipmentId,
      description: form.value.description.trim(),
    })
    // id, number и status — целиком из ответа сервера, страница их не придумывает.
    workOrders.value.unshift(created)
    form.value = emptyForm()
    router.push('/')
  } catch (error) {
    formError.value = errorMessage(error)
    if (error.kind === 'validation' && error.data?.errors) {
      // Ответ 400 по контракту: { errors: { title: ['…'], … } } — показываем у полей.
      errors.value = Object.fromEntries(
        Object.entries(error.data.errors).map(([field, messages]) => [field, messages.join(' ')]),
      )
    }
    // Поля формы не очищаем: человек исправит и отправит снова.
  } finally {
    isSubmitting.value = false
  }
}

// Фокус на первое поле при открытии страницы (autofocus срабатывает только при первой загрузке).
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <main>
    <h1>Новая заявка на ремонт</h1>
    <form class="card" novalidate :aria-busy="isSubmitting" @submit.prevent="onCreate">
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
      <p id="title-error" class="field-error">{{ errors.title }}</p>

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
      <p id="equipmentId-error" class="field-error">{{ errors.equipmentId }}</p>

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
      <p id="description-error" class="field-error">{{ errors.description }}</p>

      <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>

      <div class="actions">
        <button type="submit" class="primary" :disabled="isSubmitting">
          {{ isSubmitting ? 'Создаём…' : 'Создать' }}
        </button>
        <button type="button" :disabled="isSubmitting" @click="router.push('/')">Отмена</button>
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

.form-error {
  margin: 12px 0 0;
  padding: 10px 14px;
  border-left: 4px solid var(--danger);
  background: #fef2f2;
  color: var(--danger);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
