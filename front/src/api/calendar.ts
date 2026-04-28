import client from './client'

export interface CalendarEvent {
  fecha: string
  tipo: 'entrega' | 'inicio_curso' | 'fin_curso'
  titulo: string
  idCurso: number
}

export const getCalendarEvents = () =>
  client.get<CalendarEvent[]>('/api/calendar/events')
