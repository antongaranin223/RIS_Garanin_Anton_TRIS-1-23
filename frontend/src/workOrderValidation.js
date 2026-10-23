// Правила полей заявки на ремонт из ЛР1 (ФТ-3). Те же правила повторит сервер в ЛР8:
// клиентская проверка бережёт человека от опечатки, серверная — данные.

export const TITLE_MIN = 5
export const TITLE_MAX = 80
export const DESCRIPTION_MIN = 10
export const DESCRIPTION_MAX = 500

// Возвращает объект ошибок по полям. Пустой объект — форма корректна.
// title и description проверяются после trim: строка из пробелов не проходит.
export function validateWorkOrder({ title, equipmentId, description }) {
  const errors = {}

  const titleLength = title.trim().length
  if (titleLength < TITLE_MIN || titleLength > TITLE_MAX) {
    errors.title = `Заголовок: от ${TITLE_MIN} до ${TITLE_MAX} символов (сейчас ${titleLength})`
  }

  if (!Number.isInteger(equipmentId) || equipmentId <= 0) {
    errors.equipmentId = 'Номер оборудования: целое число больше 0'
  }

  const descriptionLength = description.trim().length
  if (descriptionLength < DESCRIPTION_MIN || descriptionLength > DESCRIPTION_MAX) {
    errors.description = `Описание: от ${DESCRIPTION_MIN} до ${DESCRIPTION_MAX} символов (сейчас ${descriptionLength})`
  }

  return errors
}
