import client from './client'
import type { Usuario } from '../types'

export const getUser = (id: number) =>
  client.get<Usuario>(`/api/users/${id}`)

export const updateUser = (id: number, data: FormData) =>
  client.put(`/api/users/${id}`, data)

export const getUsers = () =>
  client.get<any[]>('/api/users')

export const toggleUserActive = (id: number) =>
  client.put(`/api/users/${id}/toggle-active`)

export const createUser = (data: {
  nombre: string
  apellidos: string
  email: string
  password: string
  idRol: number
}) => client.post<{ idUsuario: number; message: string }>('/api/users', data)
