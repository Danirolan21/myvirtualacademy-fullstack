<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { getUser } from '../../api/users'
import { formatDate } from '../../utils/format'
import type { Usuario } from '../../types'

const auth = useAuthStore()
const usuario = ref<Usuario | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await getUser(auth.user!.id)
    usuario.value = res.data
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <section id="dashboard-page">
    <div v-if="loading" class="d-flex justify-content-center py-5">
      <div class="spinner-border"></div>
    </div>
    <template v-else-if="usuario">
      <div class="profile-section">
        <div class="profile-image-container">
          <img :src="`/assets/images/users/${auth.user?.fotoPerfil}`" class="rounded-circle" width="250" height="250" />
          <RouterLink class="edit-profile-btn" to="/perfil/editar">Edit Profile</RouterLink>
        </div>
        <h1 class="profile-heading">{{ usuario.nombre }} {{ usuario.apellidos }}</h1>
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
          <p>{{ usuario.telefono || 'No especificado' }}</p>
          <h2>Fecha de registro:</h2>
          <p>{{ formatDate(usuario.fechaRegistro) }}</p>
        </div>
      </div>
    </template>
  </section>
</template>
