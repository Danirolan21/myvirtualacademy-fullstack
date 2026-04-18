import client from './client'
import type { VistaCursosDetalles } from '../types'

export const getCourses = () =>
  client.get<VistaCursosDetalles[]>('/api/courses')

export const getCourse = (id: number) =>
  client.get<any>(`/api/courses/${id}`)

export const createCourse = (data: FormData) =>
  client.post('/api/courses', data)

export const updateCourse = (id: number, data: FormData) =>
  client.put(`/api/courses/${id}`, data)
