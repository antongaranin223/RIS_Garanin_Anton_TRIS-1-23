<script setup>
// Шапка приложения: название и меню из router-link.
// Подключается один раз в App.vue, снаружи router-view.
</script>

<template>
  <header class="app-header">
    <p class="brand">ТОиР-Цех <span>заявки на ремонт оборудования</span></p>
    <nav aria-label="Основное меню">
      <router-link to="/">Список</router-link>
      <router-link to="/new">Создать</router-link>
      <router-link to="/login">Вход</router-link>
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
  gap: 8px;
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
