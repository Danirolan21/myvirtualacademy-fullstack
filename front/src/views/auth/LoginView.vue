<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

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
  <div class="container">
    <main class="login-main">
      <div class="login-wrapper">
        <div class="login-box">
          <div class="login-decoration"></div>
          <div class="login-content">
            <h1 class="login-title">Iniciar Sesión</h1>
            <form @submit.prevent="submit">
              <div class="field">
                <label class="form-label" for="email">Email</label>
                <input v-model="email" type="email" class="form-control" id="email" placeholder="Email" required />
              </div>
              <div class="field">
                <label class="form-label" for="password">Contraseña</label>
                <input v-model="password" type="password" class="form-control" id="password" placeholder="Contraseña" required />
              </div>
              <button class="btn login-btn" :disabled="loading">
                <span v-if="loading"><i class="fas fa-spinner fa-spin"></i> Cargando…</span>
                <span v-else><i class="fas fa-sign-in-alt"></i> Iniciar Sesión</span>
              </button>
            </form>
            <div v-if="error" class="system-message mt-3">
              <h2>{{ error }}</h2>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.login-main {
  background: #f0f2f5;
  min-height: 80vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 0;
}
.login-wrapper { max-width: 500px; width: 100%; }
.login-box {
  background: white;
  border-radius: 20px;
  box-shadow: 0 20px 50px rgba(0,0,0,0.08);
  overflow: hidden;
  position: relative;
}
.login-decoration {
  position: absolute;
  top: -40px; right: -40px;
  width: 200px; height: 200px;
  background: linear-gradient(45deg, #6c5ce7, #a29bfe);
  transform: rotate(45deg);
  z-index: 1;
}
.login-content { position: relative; z-index: 2; padding: 40px; }
.login-title {
  font-size: 28px; font-weight: 800; margin-bottom: 40px; color: #2d3436;
  position: relative; display: inline-block;
}
.login-title:after {
  content: ''; position: absolute; left: 0; bottom: -10px;
  height: 4px; width: 50px;
  background: linear-gradient(45deg, #6c5ce7, #a29bfe);
  border-radius: 2px;
}
.field {
  margin-bottom: 1.25rem;
}
.field .form-control {
  height: 48px;
  background: #f7f9fc;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 0 1.25rem;
}
.field .form-control:focus {
  background: white;
  box-shadow: 0 5px 15px rgba(0,0,0,0.07);
  border-color: #a29bfe;
}
.login-btn {
  width: 100%;
  height: 52px;
  background: linear-gradient(45deg, #6c5ce7, #a29bfe);
  border-radius: 15px;
  color: white;
  font-weight: 600;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  box-shadow: 0 8px 15px rgba(108,92,231,0.25);
  transition: transform 0.2s;
}
.login-btn:hover:not(:disabled) { transform: translateY(-2px); }
.system-message {
  background: rgba(255,118,117,0.1);
  border-radius: 15px; padding: 20px;
  border-left: 4px solid #ff7675;
}
.system-message h2 { color: #d63031; font-size: 16px; font-weight: 600; margin: 0; }
</style>
