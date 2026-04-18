<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getRoles, register } from '../../api/auth'
import type { Rol } from '../../types'

const roles = ref<Rol[]>([])
const form = ref({ nombre: '', apellidos: '', email: '', password: '', idRol: 0 })
const mensaje = ref('')
const error = ref('')
const saving = ref(false)

onMounted(async () => {
  const res = await getRoles()
  roles.value = res.data
  if (roles.value.length) form.value.idRol = roles.value[0].idRol
})

async function submit() {
  error.value = ''
  mensaje.value = ''
  saving.value = true
  try {
    await register(form.value)
    mensaje.value = `Usuario ${form.value.nombre} ${form.value.apellidos} registrado correctamente.`
    form.value = { nombre: '', apellidos: '', email: '', password: '', idRol: roles.value[0]?.idRol ?? 0 }
  } catch {
    error.value = 'Error al registrar el usuario.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="container mt-3">
    <h1 class="font-monospace">Registrar Usuario</h1>
    <form @submit.prevent="submit" class="mt-3" style="max-width:500px">
      <div class="mb-2">
        <label class="form-label">Nombre</label>
        <input v-model="form.nombre" type="text" class="form-control" required />
      </div>
      <div class="mb-2">
        <label class="form-label">Apellidos</label>
        <input v-model="form.apellidos" type="text" class="form-control" required />
      </div>
      <div class="mb-2">
        <label class="form-label">Email</label>
        <input v-model="form.email" type="email" class="form-control" required />
      </div>
      <div class="mb-2">
        <label class="form-label">Contraseña</label>
        <input v-model="form.password" type="password" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Rol</label>
        <div class="d-flex gap-3 flex-wrap">
          <div v-for="rol in roles" :key="rol.idRol" class="form-check">
            <input class="form-check-input" type="radio" :id="`rol-${rol.idRol}`" :value="rol.idRol" v-model.number="form.idRol" />
            <label class="form-check-label" :for="`rol-${rol.idRol}`">{{ rol.nombre }}</label>
          </div>
        </div>
      </div>
      <button class="btn btn-success" :disabled="saving">Registrar</button>
    </form>
    <p v-if="mensaje" class="mt-3 text-success fw-bold">{{ mensaje }}</p>
    <p v-if="error" class="mt-3 text-danger fw-bold">{{ error }}</p>
  </div>
</template>
