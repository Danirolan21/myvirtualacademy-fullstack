<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)
const error = ref('')

const isDev = import.meta.env.DEV

interface DemoAccount {
  role: string
  email: string
  password: string
}

const demoAccounts: DemoAccount[] = [
  { role: 'Administrador',  email: 'admin.supremo@tajamar365.com',           password: 'admin' },
  { role: 'Profesor + Tutor', email: 'paco.garciaserrano@tajamar365.com',     password: 'paco' },
  { role: 'Estudiante',     email: 'daniel.rodriguezlancha@tajamar365.com',  password: 'lucialamejor' }
]

function useDemo(acc: DemoAccount) {
  email.value = acc.email
  password.value = acc.password
}

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const user = await auth.login(email.value, password.value)
    const redirect = route.query.redirect as string | undefined
    if (redirect) {
      router.push(redirect)
    } else if (user.isAdmin) {
      router.push('/admin')
    } else if (['Profesor', 'Tutor', 'Administrador'].includes(user.role)) {
      router.push('/profesor')
    } else {
      router.push('/estudiante')
    }
  } catch {
    error.value = 'Email o contraseña incorrectos.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="auth-page">
    <div class="auth-shell">
      <aside class="auth-brand" aria-hidden="true">
        <div class="auth-brand-deco">
          <div class="brand-shape brand-shape-1"></div>
          <div class="brand-shape brand-shape-2"></div>
          <div class="brand-shape brand-shape-3"></div>
        </div>
        <div class="auth-brand-content">
          <h1 class="brand-title">MyVirtualAcademy</h1>
          <p class="brand-tagline">Tu plataforma de aprendizaje online</p>
          <ul class="brand-bullets">
            <li><i class="fas fa-check"></i> Cursos a tu ritmo</li>
            <li><i class="fas fa-check"></i> Entrega de tareas y feedback</li>
            <li><i class="fas fa-check"></i> Profesorado especializado</li>
          </ul>
        </div>
      </aside>

      <section class="auth-form-wrapper">
        <div class="auth-form-card">
          <header class="auth-form-header">
            <h2 class="auth-form-title">Bienvenido de nuevo</h2>
            <p class="auth-form-subtitle">Accede a tu plataforma de aprendizaje</p>
          </header>

          <form class="auth-form" @submit.prevent="submit" novalidate>
            <div class="auth-field">
              <label for="email" class="form-label">Email</label>
              <div class="input-wrap">
                <i class="fas fa-envelope input-icon" aria-hidden="true"></i>
                <input
                  id="email"
                  v-model="email"
                  type="email"
                  class="form-control input-with-icon"
                  placeholder="tu@email.com"
                  autocomplete="email"
                  required
                />
              </div>
            </div>

            <div class="auth-field">
              <label for="password" class="form-label">Contraseña</label>
              <div class="input-wrap">
                <i class="fas fa-lock input-icon" aria-hidden="true"></i>
                <input
                  id="password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  class="form-control input-with-icon input-with-toggle"
                  placeholder="••••••••"
                  autocomplete="current-password"
                  required
                />
                <button
                  type="button"
                  class="input-toggle"
                  :aria-label="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                  @click="showPassword = !showPassword"
                >
                  <i :class="['fas', showPassword ? 'fa-eye-slash' : 'fa-eye']" aria-hidden="true"></i>
                </button>
              </div>
            </div>

            <div v-if="error" class="alert alert-danger auth-error" role="alert">
              <i class="fas fa-circle-exclamation" aria-hidden="true"></i>
              <span>{{ error }}</span>
            </div>

            <button
              type="submit"
              class="btn btn-primary btn-lg btn-block auth-submit"
              :disabled="loading"
            >
              <i v-if="loading" class="fas fa-spinner fa-spin" aria-hidden="true"></i>
              <i v-else class="fas fa-sign-in-alt" aria-hidden="true"></i>
              <span>{{ loading ? 'Iniciando sesión…' : 'Iniciar sesión' }}</span>
            </button>
          </form>

          <p class="auth-form-footer">
            ¿No tienes cuenta?
            <router-link to="/register" class="auth-link">Crear cuenta</router-link>
          </p>

          <details v-if="isDev" class="auth-demo">
            <summary class="auth-demo-summary">
              <i class="fas fa-flask" aria-hidden="true"></i>
              <span>Credenciales de demo (solo en desarrollo)</span>
            </summary>
            <div class="auth-demo-body">
              <button
                v-for="acc in demoAccounts"
                :key="acc.email"
                type="button"
                class="demo-account"
                @click="useDemo(acc)"
              >
                <div class="demo-account-info">
                  <strong>{{ acc.role }}</strong>
                  <span class="demo-account-email">{{ acc.email }}</span>
                </div>
                <span class="demo-account-cta">Usar</span>
              </button>
            </div>
          </details>
        </div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.auth-page {
  min-height: calc(100vh - var(--navbar-height));
  display: flex;
  background: var(--color-page-bg);
}

.auth-shell {
  width: 100%;
  display: grid;
  grid-template-columns: 40% 60%;
}

@media (max-width: 900px) {
  .auth-shell { grid-template-columns: 1fr; }
  .auth-brand { display: none; }
}

/* ─── Brand panel (left) ─── */
.auth-brand {
  position: relative;
  background: var(--profile-header-gradient);
  color: #fff;
  padding: var(--sp-12);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}
.auth-brand-deco { position: absolute; inset: 0; pointer-events: none; }
.brand-shape {
  position: absolute;
  border-radius: var(--radius-full);
  background: rgba(255, 255, 255, 0.06);
}
.brand-shape-1 { width: 360px; height: 360px; top: -120px; right: -100px; }
.brand-shape-2 { width: 220px; height: 220px; bottom: -80px; left: -60px; background: rgba(255,255,255,0.04); }
.brand-shape-3 { width: 140px; height: 140px; top: 50%; left: 14%; background: rgba(255,255,255,0.03); }

.auth-brand-content { position: relative; z-index: 1; max-width: 380px; }
.brand-title {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-3);
  letter-spacing: -0.02em;
}
.brand-tagline {
  font-size: var(--font-size-lg);
  opacity: 0.85;
  margin: 0 0 var(--sp-10);
  line-height: 1.5;
}
.brand-bullets {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: var(--sp-3);
  font-size: var(--font-size-md);
}
.brand-bullets li {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  opacity: 0.9;
}
.brand-bullets i {
  width: 28px;
  height: 28px;
  border-radius: var(--radius-full);
  background: rgba(255, 255, 255, 0.15);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.75rem;
  flex-shrink: 0;
}

/* ─── Form panel (right) ─── */
.auth-form-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--sp-8) var(--sp-6);
}
.auth-form-card {
  width: 100%;
  max-width: 420px;
}
.auth-form-header { margin-bottom: var(--sp-8); }
.auth-form-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-2);
  color: var(--color-text);
}
.auth-form-subtitle {
  margin: 0;
  color: var(--color-muted);
  font-size: var(--font-size-sm);
}

.auth-form { display: grid; gap: var(--sp-4); }
.auth-field { display: grid; gap: var(--sp-1); }

.input-wrap { position: relative; }
.input-icon {
  position: absolute;
  left: var(--sp-3);
  top: 50%;
  transform: translateY(-50%);
  color: var(--color-muted);
  font-size: 0.875rem;
  pointer-events: none;
}
.input-with-icon { padding-left: 2.25rem; height: 44px; }
.input-with-toggle { padding-right: 2.5rem; }

.input-toggle {
  position: absolute;
  right: var(--sp-2);
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  color: var(--color-muted);
  cursor: pointer;
  padding: var(--sp-2);
  border-radius: var(--radius-md);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: color 0.15s, background 0.15s;
}
.input-toggle:hover { color: var(--color-primary); background: var(--color-muted-bg); }

.auth-error { align-items: center; }
.auth-submit { height: 48px; margin-top: var(--sp-2); gap: var(--sp-2); }

.auth-form-footer {
  margin: var(--sp-6) 0 0;
  text-align: center;
  font-size: var(--font-size-sm);
  color: var(--color-muted);
}
.auth-link {
  font-weight: var(--font-weight-semibold);
  color: var(--color-primary);
}

/* ─── Demo section ─── */
.auth-demo {
  margin-top: var(--sp-6);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-muted-bg);
}
.auth-demo-summary {
  cursor: pointer;
  padding: var(--sp-3) var(--sp-4);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  list-style: none;
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}
.auth-demo-summary::-webkit-details-marker { display: none; }
.auth-demo-summary::marker { content: ''; }
.auth-demo[open] .auth-demo-summary { border-bottom: 1px solid var(--color-border); }

.auth-demo-body {
  padding: var(--sp-3);
  display: grid;
  gap: var(--sp-2);
}
.demo-account {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--sp-3) var(--sp-4);
  cursor: pointer;
  transition: border-color 0.15s, box-shadow 0.15s;
  text-align: left;
  font-family: inherit;
}
.demo-account:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}
.demo-account-info { display: flex; flex-direction: column; gap: 2px; }
.demo-account-info strong { font-size: var(--font-size-sm); color: var(--color-text); }
.demo-account-email { font-size: var(--font-size-xs); color: var(--color-muted); }
.demo-account-cta {
  font-size: var(--font-size-xs);
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
  flex-shrink: 0;
  margin-left: var(--sp-3);
}
</style>
