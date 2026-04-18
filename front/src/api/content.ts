import client from './client'
import type { RecursoViewModel } from '../types'

export const getContent = (id: number) =>
  client.get<RecursoViewModel>(`/api/content/${id}`)

export const createContent = (data: FormData) =>
  client.post('/api/content', data)

export const updateContent = (id: number, data: FormData) =>
  client.put(`/api/content/${id}`, data)

export const checkAccess = (id: number) =>
  client.get<boolean>(`/api/content/${id}/access`)

export const registerView = (id: number) =>
  client.post(`/api/content/${id}/view`)
