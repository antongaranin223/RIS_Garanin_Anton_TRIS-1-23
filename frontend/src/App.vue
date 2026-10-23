<script setup>
import AppHeader from './components/AppHeader.vue'
</script>

<template>
  <!-- Шапка снаружи router-view: она одна и видна на всех адресах -->
  <AppHeader />
  <!-- Сюда роутер подставляет страницу текущего адреса: /, /new или /login -->
  <router-view />
</template>
