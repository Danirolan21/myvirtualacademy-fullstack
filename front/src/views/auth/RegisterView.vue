<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const nombre = ref('')
const apellidos = ref('')
const email = ref('')
const password = ref('')
const passwordConfirm = ref('')
const showPassword = ref(false)
const showPasswordConfirm = ref(false)

const loading = ref(false)
const error = ref('')
const successMessage = ref('')

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/* ─── Password strength ─── */
const passwordStrength = computed<number>(() => {
  const p = password.value
  if (!p) return 0
  let score = 0
  if (p.length >= 6) score++
  if (p.length >= 10) score++
  if (/[a-z]/.test(p) && /[A-Z]/.test(p)) score++
  if (/\d/.test(p)) score++
  if (/[^a-zA-Z\d]/.test(p)) score++
  return Math.min(score, 4)
})

const strengthLabel = computed(() => {
  switch (passwordStrength.value) {
    case 0: return ''
    case 1: return 'Débil'
    case 2: return 'Aceptable'
    case 3: return 'Buena'
    default: return 'Fuerte'
  }
})

const passwordsMismatch = computed(() =>
  password.value.length > 0 &&
  passwordConfirm.value.length > 0 &&
  password.value !== passwordConfirm.value
)

async function submit() {
  error.value = ''
  successMessage.value = ''

  if (!nombre.value.trim())                    { error.value = 'El nombre es obligatorio'; return }
  if (!apellidos.value.trim())                 { error.value = 'Los apellidos son obligatorios'; return }
  if (!emailRegex.test(email.value.trim()))    { error.value = 'El email no es válido'; return }
  if (password.value.length < 6)               { error.value = 'La contraseña debe tener al menos 6 caracteres'; return }
  if (password.value !== passwordConfirm.value) { error.value = 'Las contraseñas no coinciden'; return }

  loading.value = true
  try {
    await auth.register(nombre.value.trim(), apellidos.value.trim(), email.value.trim(), password.value)

    // Auto-login con las mismas credenciales
    try {
      await auth.login(email.value.trim(), password.value)
      router.push('/estudiante')
    } catch {
      // Fallback: registro OK pero login falló → mensaje + redirect a /login
      successMessage.value = '¡Cuenta creada correctamente! Te redirigimos al inicio de sesión…'
      setTimeout(() => router.push('/login'), 2000)
    }
  } catch (e: unknown) {
    const errObj = e as { response?: { data?: { message?: string } } } | undefined
    error.value = errObj?.response?.data?.message ?? 'No se pudo crear la cuenta. Inténtalo de nuevo.'
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
            <h2 class="auth-form-title">Crear cuenta</h2>
            <p class="auth-form-subtitle">Únete a MyVirtualAcademy</p>
          </header>

          <form class="auth-form" @submit.prevent="submit" novalidate>
            <div class="auth-row">
              <div class="auth-field">
                <label for="nombre" class="form-label">Nombre</label>
                <div class="input-wrap">
                  <i class="fas fa-user input-icon" aria-hidden="true"></i>
                  <input
                    id="nombre"
                    v-model="nombre"
                    type="text"
                    class="form-control input-with-icon"
                    placeholder="Tu nombre"
                    autocomplete="given-name"
                    required
                  />
                </div>
              </div>

              <div class="auth-field">
                <label for="apellidos" class="form-label">Apellidos</label>
                <div class="input-wrap">
                  <i class="fas fa-id-card input-icon" aria-hidden="true"></i>
                  <input
                    id="apellidos"
                    v-model="apellidos"
                    type="text"
                    class="form-control input-with-icon"
                    placeholder="Tus apellidos"
                    autocomplete="family-name"
                    required
                  />
                </div>
              </div>
            </div>

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
                  placeholder="Mínimo 6 caracteres"
                  autocomplete="new-password"
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
              <div v-if="password" class="strength">
                <div class="strength-bar">
                  <div
                    v-for="n in 4"
                    :key="n"
                    class="strength-segment"
                    :class="[`strength-segment-${passwordStrength}`, { 'is-active': n <= passwordStrength }]"
                  ></div>
                </div>
                <span class="strength-label" :class="`strength-label-${passwordStrength}`">{{ strengthLabel }}</span>
              </div>
            </div>

            <div class="auth-field">
              <label for="password-confirm" class="form-label">Confirmar contraseña</label>
              <div class="input-wrap">
                <i class="fas fa-lock input-icon" aria-hidden="true"></i>
                <input
                  id="password-confirm"
                  v-model="passwordConfirm"
                  :type="showPasswordConfirm ? 'text' : 'password'"
                  class="form-control input-with-icon input-with-toggle"
                  placeholder="Repite la contraseña"
                  autocomplete="new-password"
                  required
                />
                <button
                  type="button"
                  class="input-toggle"
                  :aria-label="showPasswordConfirm ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                  @click="showPasswordConfirm = !showPasswordConfirm"
                >
                  <i :class="['fas', showPasswordConfirm ? 'fa-eye-slash' : 'fa-eye']" aria-hidden="true"></i>
                </button>
              </div>
              <p v-if="passwordsMismatch" class="form-text mismatch">
                <i class="fas fa-circle-exclamation" aria-hidden="true"></i>
                Las contraseñas no coinciden
              </p>
            </div>

            <div v-if="successMessage" class="alert alert-success auth-success" role="status">
              <i class="fas fa-circle-check" aria-hidden="true"></i>
              <span>{{ successMessage }}</span>
            </div>
            <div v-else-if="error" class="alert alert-danger auth-error" role="alert">
              <i class="fas fa-circle-exclamation" aria-hidden="true"></i>
              <span>{{ error }}</span>
            </div>

            <button
              type="submit"
              class="btn btn-primary btn-lg btn-block auth-submit"
              :disabled="loading || !!successMessage"
            >
              <i v-if="loading" class="fas fa-spinner fa-spin" aria-hidden="true"></i>
              <i v-else class="fas fa-user-plus" aria-hidden="true"></i>
              <span>{{ loading ? 'Creando cuenta…' : 'Crear cuenta' }}</span>
            </button>
          </form>

          <p class="auth-form-footer">
            ¿Ya tienes cuenta?
            <router-link to="/login" class="auth-link">Iniciar sesión</router-link>
          </p>
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
  width: 28px; height: 28px;
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
  max-width: 460px;
}
.auth-form-header { margin-bottom: var(--sp-6); }
.auth-form-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-2);
  color: var(--color-text);
}
.auth-form-subtitle { margin: 0; color: var(--color-muted); font-size: var(--font-size-sm); }

.auth-form { display: grid; gap: var(--sp-4); }
.auth-field { display: grid; gap: var(--sp-1); }

.auth-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-3);
}
@media (max-width: 480px) {
  .auth-row { grid-template-columns: 1fr; }
}

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

/* ─── Strength bar ─── */
.strength {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  margin-top: var(--sp-1);
}
.strength-bar {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 4px;
}
.strength-segment {
  height: 4px;
  border-radius: var(--radius-full);
  background: var(--color-border);
  transition: background 0.2s;
}
.strength-segment.is-active.strength-segment-1 { background: var(--color-danger); }
.strength-segment.is-active.strength-segment-2 { background: var(--color-warning); }
.strength-segment.is-active.strength-segment-3 { background: var(--color-info); }
.strength-segment.is-active.strength-segment-4 { background: var(--color-success); }
.strength-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  min-width: 4.5rem;
  text-align: right;
  color: var(--color-muted);
}
.strength-label-1 { color: var(--color-danger); }
.strength-label-2 { color: #b07000; }
.strength-label-3 { color: #0a7990; }
.strength-label-4 { color: var(--color-success); }

.mismatch {
  display: flex;
  align-items: center;
  gap: var(--sp-1);
  color: var(--color-danger);
  margin-top: var(--sp-1);
}

.auth-error,
.auth-success { align-items: center; }
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
</style>
