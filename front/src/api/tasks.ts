import client from './client'
import type { TareaViewModel } from '../types'

export const getTask = (id: number) =>
  client.get<TareaViewModel>(`/api/tasks/${id}`)

export const submitTask = (id: number, data: FormData) =>
  client.post(`/api/tasks/${id}/submit`, data)

export const gradeTask = (id: number, data: { idEntrega: number; idEstudiante: number; calificacion: number; comentarios?: string }) =>
  client.post(`/api/tasks/${id}/grade`, data)
