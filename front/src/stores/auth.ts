import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'
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
      const res = await axios.post('/api/auth/refresh', {}, { withCredentials: true })
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
    const res = await axios.post('/api/auth/login',
      { email, password }, { withCredentials: true })
    setSession(res.data.accessToken, res.data.user)
    return res.data.user as AuthUser
  }

  async function logout() {
    try {
      await axios.post('/api/auth/logout', {}, { withCredentials: true })
    } finally {
      clearSession()
    }
  }

  return {
    accessToken, user, isAuthenticated, isAdmin, role, isLoading,
    login, logout, silentRefresh, setSession, clearSession
  }
})
