import { createRouter, createWebHistory } from 'vue-router'
import WorkOrderList from './pages/WorkOrderList.vue'
import WorkOrderNew from './pages/WorkOrderNew.vue'
import LoginPage from './pages/LoginPage.vue'
import { currentUser } from './api/session.js'
import { setSessionExpiredHandler } from './api/http.js'

// Маршруты Vue — какой экран показать человеку. Это не запросы к серверу.
// meta.auth — экран только после входа; meta.roles — кому показывать экран.
// Это удобство, а не защита: права проверяет сервер (401/403), даже если открыть API в обход экрана.
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: WorkOrderList, meta: { auth: true } },
    { path: '/new', component: WorkOrderNew, meta: { auth: true, roles: ['Foreman', 'Mechanic'] } },
    { path: '/login', component: LoginPage },
  ],
})

router.beforeEach((to) => {
  if (to.meta.auth && !currentUser.value) {
    // Не вошёл — на вход, а после входа вернуть туда, куда шёл.
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.roles && !to.meta.roles.includes(currentUser.value.role)) {
    return { path: '/' }
  }
})

// Сервер ответил 401 на запрос с токеном (токен истёк) — на экран входа.
setSessionExpiredHandler(() => {
  const here = router.currentRoute.value
  if (here.path !== '/login') router.push({ path: '/login', query: { redirect: here.fullPath } })
})
