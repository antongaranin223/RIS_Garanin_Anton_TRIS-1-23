import { createRouter, createWebHistory } from 'vue-router'
import WorkOrderList from './pages/WorkOrderList.vue'
import WorkOrderNew from './pages/WorkOrderNew.vue'
import LoginPage from './pages/LoginPage.vue'

// Маршруты Vue — какой экран показать человеку.
// Это не запросы к серверу: POST /api/work-orders появится позже на порту 5000.
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: WorkOrderList },
    { path: '/new', component: WorkOrderNew },
    { path: '/login', component: LoginPage },
  ],
})
