<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getUser, updateUser } from '../../api/users'
import type { Usuario } from '../../types'

const auth = useAuthStore()
const router = useRouter()
const usuario = ref<Usuario | null>(null)
const loading = ref(true)
const saving = ref(false)
const avatarFile = ref<File | null>(null)

onMounted(async () => {
  try {
    const res = await getUser(auth.user!.id)
    usuario.value = res.data
  } finally {
    loading.value = false
  }
})

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
  <section id="dashboard-page">
    <div v-if="loading" class="d-flex justify-content-center py-5">
      <div class="spinner-border"></div>
    </div>
    <form v-else-if="usuario" @submit.prevent="submit" enctype="multipart/form-data">
      <div class="profile-section">
        <div class="profile-image-container">
          <div class="position-relative">
            <img :src="`/assets/images/users/${auth.user?.fotoPerfil}`" class="rounded-circle" width="250" height="250" />
            <label for="profile-pic-upload" class="position-absolute bottom-0 end-0 bg-white rounded-circle p-2" style="cursor:pointer;z-index:3">
              <i class="fas fa-pencil-alt"></i>
            </label>
            <input type="file" id="profile-pic-upload" accept="image/*" style="display:none"
              @change="e => avatarFile = (e.target as HTMLInputElement).files?.[0] ?? null" />
          </div>
          <button type="submit" class="edit-profile-btn" :disabled="saving">Guardar cambios</button>
        </div>
        <h1 class="profile-heading">
          <input v-model="usuario.nombre" type="text" class="bg-transparent border-0 text-white w-100" style="font-size:inherit;font-weight:inherit;font-family:inherit" required />
        </h1>
        <h1 class="profile-heading">
          <input v-model="usuario.apellidos" type="text" class="bg-transparent border-0 text-white w-100" style="font-size:inherit;font-weight:inherit;font-family:inherit" required />
        </h1>
        <h1 class="profile-heading">{{ auth.role }}</h1>
      </div>
      <div class="container profile-info d-flex justify-content-between gap-1">
        <div class="border-black w-50 ps-2">
          <h1 class="mb-1">Mis <span class="text-danger fst-italic">Cursos</span></h1>
        </div>
        <div class="border-black w-50 ps-2" style="margin-top:-125px">
          <h1 class="mb-1">Más <span class="text-danger fst-italic">datos</span></h1>
          <h2>Correo:</h2>
          <p>{{ usuario.email }}</p>
          <h2>Teléfono:</h2>
          <div class="mb-3">
            <input v-model="usuario.telefono" type="tel" class="form-control" placeholder="Número de teléfono" />
          </div>
          <h2>Fecha de registro:</h2>
          <p>{{ new Date(usuario.fechaRegistro).toLocaleDateString('es-ES') }}</p>
        </div>
      </div>
    </form>
  </section>
</template>
