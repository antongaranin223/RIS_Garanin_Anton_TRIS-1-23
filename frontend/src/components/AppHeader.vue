<script setup>
// Шапка приложения: название, меню и кто вошёл (ФТ-1: имя и роль в шапке).
// Подключается один раз в App.vue, снаружи router-view.
import { useRouter } from 'vue-router'
import { logout } from '../api/auth.js'
import { canCreate, currentUser, roleLabels } from '../api/session.js'
import { workOrders } from '../workOrdersStore.js'

const router = useRouter()

function onLogout() {
  logout()
  workOrders.value = [] // данные прошлого пользователя на экране не оставляем
  router.push('/login')
}
</script>

<template>
  <header class="app-header">
    <p class="brand">ТОиР-Цех <span>заявки на ремонт оборудования</span></p>
    <nav aria-label="Основное меню">
      <template v-if="currentUser">
        <router-link to="/">Список</router-link>
        <!-- Слесарю и начальнику цеха кнопку не показываем, но запрещает всё равно сервер (403) -->
        <router-link v-if="canCreate" to="/new">Создать</router-link>
        <span class="user">
          {{ currentUser.fullName }} <span class="role">· {{ roleLabels[currentUser.role] }}</span>
        </span>
        <button type="button" class="logout" @click="onLogout">Выйти</button>
      </template>
      <router-link v-else to="/login">Вход</router-link>
    </nav>
  </header>
</template>

<style scoped>
.app-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 24px;
  background: var(--header-bg);
  color: var(--header-text);
}

.brand {
  margin: 0;
  font-weight: 700;
  font-size: 1.1rem;
}

.brand span {
  margin-left: 8px;
  font-weight: 400;
  font-size: 0.9rem;
  opacity: 0.8;
}

nav {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.user {
  margin-left: 8px;
  font-weight: 600;
}

.role {
  font-weight: 400;
  opacity: 0.85;
}

.logout {
  padding: 8px 16px;
  border: 1px solid rgb(255 255 255 / 0.4);
  border-radius: 6px;
  background: transparent;
  color: var(--header-text);
  cursor: pointer;
}

.logout:hover {
  border-color: var(--header-text);
}

nav a {
  padding: 8px 16px;
  border: 1px solid rgb(255 255 255 / 0.4);
  border-radius: 6px;
  color: var(--header-text);
  text-decoration: none;
}

nav a:hover {
  border-color: var(--header-text);
}

/* router-link сам ставит aria-current="page" на ссылку текущего адреса */
nav a[aria-current='page'] {
  background: var(--header-text);
  border-color: var(--header-text);
  color: var(--header-bg);
}
</style>
