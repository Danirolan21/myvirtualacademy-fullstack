import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import client from '../api/client'
import type { AuthUser } from '../types'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(sessionStorage.getItem('__mva_at'))
  const user = ref<AuthUser | null>(null)
  const isLoading = ref(false)

  const isAuthenticated = computed(() => !!accessToken.value)
  const isAdmin = computed(() => user.value?.isAdmin ?? false)
  const role = computed(() => user.value?.role ?? '')

  function setSession(token: string, userData: AuthUser) {
    accessToken.value = token
    user.value = userData
    sessionStorage.setItem('__mva_at', token)
  }

  function clearSession() {
    accessToken.value = null
    user.value = null
    sessionStorage.removeItem('__mva_at')
  }

  async function silentRefresh(): Promise<boolean> {
    isLoading.value = true
    try {
      const res = await client.post('/api/auth/refresh', {})
      setSession(res.data.accessToken, res.data.user)
      return true
    } catch {
      clearSession()
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function login(email: string, password: string) {
    const res = await client.post('/api/auth/login', { email, password })
    setSession(res.data.accessToken, res.data.user)
    return res.data.user as AuthUser
  }

  async function register(nombre: string, apellidos: string, email: string, password: string) {
    await client.post('/api/auth/register', { nombre, apellidos, email, password })
  }

  async function logout() {
    try {
      await client.post('/api/auth/logout', {})
    } finally {
      clearSession()
    }
  }

  return {
    accessToken, user, isAuthenticated, isAdmin, role, isLoading,
    login, register, logout, silentRefresh, setSession, clearSession
  }
})