import client from './client'

export interface Notificacion {
  idNotificacion: number
  tipo: 'entrega' | 'calificacion' | 'inscripcion'
  titulo: string
  mensaje: string
  leida: boolean
  fechaCreacion: string
}

export const getNotifications = (page = 1) =>
  client.get<Notificacion[]>('/api/notifications', { params: { page, pageSize: 20 } })

export const getUnreadCount = () =>
  client.get<{ count: number }>('/api/notifications/unread-count')

export const markRead = (id: number) =>
  client.post(`/api/notifications/${id}/read`)

export const markAllRead = () =>
  client.post('/api/notifications/read-all')
