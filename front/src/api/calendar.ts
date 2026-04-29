import client from './client'

export interface CalendarEvent {
  fecha: string
  tipo: 'entrega' | 'inicio_curso' | 'fin_curso'
  tipoContenido?: string
  titulo: string
  idCurso: number
}

export const getCalendarEvents = () =>
  client.get<CalendarEvent[]>('/api/calendar/events')
