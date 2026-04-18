import client from './client'
import type { Usuario } from '../types'

export const getUser = (id: number) =>
  client.get<Usuario>(`/api/users/${id}`)

export const updateUser = (id: number, data: FormData) =>
  client.put(`/api/users/${id}`, data)
