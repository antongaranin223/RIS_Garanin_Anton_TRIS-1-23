import { ref } from 'vue'

// Единственный общий массив заявок для всех страниц клиента.
// Заполняется ответом getWorkOrders() из src/api/workOrders.js; своих тестовых данных и
// своего счётчика id больше нет — id, number и status приходят из ответа API.
export const workOrders = ref([])
