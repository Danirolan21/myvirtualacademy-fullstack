import client from './client'
import type { AuthUser, Rol } from '../types'

export const login = (email: string, password: string) =>
  client.post<{ accessToken: string; user: AuthUser }>('/api/auth/login', { email, password })

export const logout = () => client.post('/api/auth/logout')

export const register = (data: {
  nombre: string
  apellidos: string
  email: string
  password: string
  idRol: number
}) => client.post('/api/auth/register', data)

export const getRoles = () => client.get<Rol[]>('/api/auth/roles')
