import client from './client'
import type { RecursoViewModel } from '../types'

export const getContent = (id: number) =>
  client.get<RecursoViewModel>(`/api/content/${id}`)

export const createContent = (data: FormData) =>
  client.post<{ idContenido: number; idAsignatura: number; message: string }>('/api/content', data)

export const updateContent = (id: number, data: FormData) =>
  client.put(`/api/content/${id}`, data)

export const checkAccess = async (id: number): Promise<boolean> => {
  const res = await client.get<{ hasAccess: boolean }>(`/api/content/${id}/access`)
  return res.data.hasAccess
}

export const registerView = (id: number) =>
  client.post(`/api/content/${id}/view`)

export const deleteContent = (id: number) =>
  client.delete(`/api/content/${id}`)
