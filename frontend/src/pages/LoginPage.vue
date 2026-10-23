<script setup>
import { ref } from 'vue'

// Поля формы — тело будущего POST /api/auth/login.
const credentials = ref({ login: '', password: '' })

// Сообщение-заглушка: настоящего входа пока нет.
const notice = ref('')

function submitLogin() {
  notice.value = 'Вход пока не проверяется: авторизация появится в следующих работах.'
}

// Фокус на поле логина при открытии страницы.
const vFocus = { mounted: (el) => el.focus() }
</script>

<template>
  <main>
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
