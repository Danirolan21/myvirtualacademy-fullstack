<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getUser, updateUser } from '../../api/users'
import { formatDate } from '../../utils/format'
import type { Usuario } from '../../types'
import { userAvatar } from '../../utils/images'

const auth = useAuthStore()
const router = useRouter()
const usuario = ref<Usuario | null>(null)
const loading = ref(true)
const saving = ref(false)
const avatarFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)

onMounted(async () => {
  try {
    const res = await getUser(auth.user!.id)
    usuario.value = res.data
  } finally {
    loading.value = false
  }
})

function onAvatarChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0] ?? null
  avatarFile.value = file
  if (file) avatarPreview.value = URL.createObjectURL(file)
}

async function submit() {
  if (!usuario.value) return
  saving.value = true
  try {
    const fd = new FormData()
    fd.append('nombre', usuario.value.nombre)
    fd.append('apellidos', usuario.value.apellidos)
    fd.append('telefono', usuario.value.telefono ?? '')
    if (avatarFile.value) fd.append('fotoPerfil', avatarFile.value)
    await updateUser(auth.user!.id, fd)
    router.push('/perfil')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

    <form v-else-if="usuario" @submit.prevent="submit">

      <!-- Dark profile header (editable) -->
      <div class="profile-hero">
        <div class="profile-hero-bg"></div>
        <div class="profile-hero-content container">

          <!-- Avatar with upload pencil -->
          <div class="avatar-wrap">
            <img
              :src="avatarPreview ?? userAvatar(auth.user?.fotoPerfil)"
              class="avatar-img"
              alt="Avatar"
            />
            <label class="avatar-edit-btn" for="avatar-upload" title="Cambiar foto">
              <i class="fas fa-pencil-alt"></i>
            </label>
            <input
              id="avatar-upload"
              type="file"
              accept="image/*"
              class="visually-hidden"
              @change="onAvatarChange"
            />
          </div>

          <!-- Editable name fields -->
          <div class="profile-hero-info">
            <input
              v-model="usuario.nombre"
              type="text"
              class="hero-input"
              placeholder="Nombre"
              required
            />
            <input
              v-model="usuario.apellidos"
              type="text"
              class="hero-input hero-input-sub"
              placeholder="Apellidos"
              required
            />
            <span class="profile-role-badge">{{ auth.role }}</span>
          </div>

          <!-- Save button in header -->
          <button type="submit" class="btn-save-hero" :disabled="saving">
            <span v-if="saving"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
            <span v-else><i class="fas fa-save"></i> Guardar cambios</span>
          </button>

        </div>
      </div>

      <!-- Two-column body -->
      <div class="container profile-body">

        <!-- Left: Mis Cursos (read-only in edit too) -->
        <section class="profile-section">
          <h2 class="section-heading">Mis <span class="heading-accent">Cursos</span></h2>
          <div class="empty-courses">
            <i class="fas fa-book-open empty-icon"></i>
            <p>No tienes cursos asignados actualmente.</p>
          </div>
        </section>

        <!-- Right: Editable data -->
        <section class="profile-section">
          <h2 class="section-heading">Más <span class="heading-accent">datos</span></h2>
          <dl class="data-list">
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-envelope"></i> Email</dt>
              <dd class="data-value">{{ usuario.email }}</dd>
            </div>
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-phone"></i> Teléfono</dt>
              <dd class="data-value">
                <input
                  v-model="usuario.telefono"
                  type="tel"
                  class="form-control form-control-sm"
                  placeholder="Número de teléfono"
                />
              </dd>
            </div>
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-calendar-plus"></i> Registro</dt>
              <dd class="data-value">{{ formatDate(usuario.fechaRegistro) }}</dd>
            </div>
          </dl>

          <div class="form-actions">
            <RouterLink to="/perfil" class="btn btn-outline-secondary">
              <i class="fas fa-times"></i> Cancelar
            </RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              <span v-if="saving"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
              <span v-else><i class="fas fa-save"></i> Guardar cambios</span>
            </button>
          </div>
        </section>

      </div>
    </form>
  </div>
</template>

<style scoped>
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
.visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; }

/* Hero */
.profile-hero {
  position: relative;
  background: #1a1a2e;
  overflow: hidden;
  padding: var(--sp-10) 0 var(--sp-8);
}
.profile-hero-bg {
  position: absolute;
  inset: 0;
  background:
    url('/assets/images/3d-background-neon-ultraviolet-purple-3840x1080-2562.jpg') center / cover no-repeat;
  opacity: 0.25;
}
.profile-hero-content {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--sp-6);
  flex-wrap: wrap;
}

.avatar-wrap { position: relative; flex-shrink: 0; }
.avatar-img {
  width: 120px; height: 120px;
  border-radius: 50%;
  object-fit: cover;
  border: 4px solid rgba(255,255,255,0.3);
}
.avatar-edit-btn {
  position: absolute;
  bottom: 4px; right: 4px;
  width: 28px; height: 28px;
  background: var(--color-primary);
  color: white;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  cursor: pointer;
  transition: background 0.15s;
}
.avatar-edit-btn:hover { background: #0b5ed7; }

.profile-hero-info { flex: 1; display: flex; flex-direction: column; gap: var(--sp-2); }

.hero-input {
  background: transparent;
  border: none;
  border-bottom: 1px solid rgba(255,255,255,0.4);
  color: white;
  font-family: var(--font-sans);
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  padding: var(--sp-1) 0;
  width: 100%;
  outline: none;
  transition: border-color 0.15s;
}
.hero-input:focus { border-bottom-color: rgba(255,255,255,0.9); }
.hero-input::placeholder { color: rgba(255,255,255,0.4); }
.hero-input-sub { font-size: var(--font-size-xl); font-weight: var(--font-weight-semibold); }

.profile-role-badge {
  display: inline-block;
  background: rgba(255,255,255,0.15);
  color: rgba(255,255,255,0.9);
  border: 1px solid rgba(255,255,255,0.25);
  border-radius: 999px;
  padding: var(--sp-1) var(--sp-3);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  width: fit-content;
}

.btn-save-hero {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-2) var(--sp-4);
  background: var(--color-primary);
  border: none;
  border-radius: var(--radius-md);
  color: white;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  transition: background 0.15s;
  align-self: flex-start;
}
.btn-save-hero:hover:not(:disabled) { background: #0b5ed7; }
.btn-save-hero:disabled { opacity: 0.6; cursor: not-allowed; }

/* Body */
.profile-body {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-8);
  padding-top: var(--sp-8);
  padding-bottom: var(--sp-8);
}

.section-heading {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-5);
  color: var(--color-text);
}
.heading-accent { color: var(--color-danger); font-style: italic; }

.empty-courses {
  text-align: center;
  padding: var(--sp-10) var(--sp-4);
  color: var(--color-muted);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-lg);
}
.empty-icon { font-size: 2rem; margin-bottom: var(--sp-3); display: block; }

.data-list { margin: 0 0 var(--sp-6); }
.data-row {
  display: grid;
  grid-template-columns: 160px minmax(0, 1fr);
  gap: var(--sp-2);
  padding: var(--sp-3) 0;
  border-bottom: 1px solid var(--color-border);
  align-items: center;
}
.data-row:first-child { border-top: 1px solid var(--color-border); }
.data-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}
.data-label i { width: 14px; text-align: center; color: var(--color-muted); }
.data-value {
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  margin: 0;
  overflow-wrap: anywhere;
  word-break: break-word;
  min-width: 0;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--sp-3);
}

@media (max-width: 768px) {
  .profile-body { grid-template-columns: 1fr; }
  .profile-hero-content { flex-direction: column; text-align: center; }
  .avatar-wrap { margin: 0 auto; }
  .hero-input { text-align: center; }
}
</style>
