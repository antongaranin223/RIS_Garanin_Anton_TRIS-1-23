<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { login } from '../api/auth.js'
import { errorMessage } from '../api/http.js'

const router = useRouter()
const route = useRoute()

// Поля формы — тело POST /api/auth/login.
const credentials = ref({ login: '', password: '' })
const errorText = ref('')
const isSubmitting = ref(false)

async function submitLogin() {
  if (isSubmitting.value) return
  errorText.value = ''
  if (!credentials.value.login.trim() || !credentials.value.password) {
    errorText.value = 'Введите логин и пароль.'
    return
  }

  isSubmitting.value = true
  try {
    await login(credentials.value.login.trim(), credentials.value.password)
    // После входа — туда, куда человек шёл, или в список (ФТ-1). Только адреса этого приложения.
    const redirect = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/')
      ? route.query.redirect
      : '/'
    router.push(redirect)
  } catch (error) {
    // 401 → «Неверный логин или пароль» (текст сервера), сеть → «Нет связи с сервером».
    errorText.value = errorMessage(error)
    credentials.value.password = '' // пароль после отказа не держим в форме
  } finally {
    isSubmitting.value = false
  }
}

// Фокус на поле логина при открытии страницы.
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <main>
    <h1>Вход в систему</h1>
    <form class="card narrow" :aria-busy="isSubmitting" @submit.prevent="submitLogin">
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

      <p v-if="errorText" class="form-error" role="alert">{{ errorText }}</p>

      <div class="actions">
        <button type="submit" class="primary" :disabled="isSubmitting">
          {{ isSubmitting ? 'Входим…' : 'Войти' }}
        </button>
      </div>
    </form>
  </main>
</template>

<style scoped>
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
