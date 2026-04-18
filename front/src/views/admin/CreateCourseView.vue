<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { createCourse } from '../../api/courses'
import client from '../../api/client'
import FileUploader from '../../components/FileUploader.vue'

const router = useRouter()

const ESTADOS = ['Borrador', 'Activo', 'Finalizado', 'Archivado', 'Suspendido']

const form = ref({
  nombre: '',
  descripcion: '',
  idProfesor: '',
  fechaInicio: '',
  fechaFin: '',
  estado: 'Borrador',
})
const profesores = ref<any[]>([])
const coverFile = ref<File | null>(null)
const saving = ref(false)
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await client.get('/api/users/profesores')
    profesores.value = res.data
  } finally {
    loading.value = false
  }
})

async function submit() {
  saving.value = true
  try {
    const fd = new FormData()
    Object.entries(form.value).forEach(([k, v]) => fd.append(k, String(v)))
    if (coverFile.value) fd.append('imagenPortada', coverFile.value)
    await createCourse(fd)
    router.push('/admin')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <main class="container mt-3">
    <div class="course-header mb-4">
      <h1>Crear Nuevo Curso</h1>
      <p class="text-muted">Complete el formulario para crear un nuevo curso en la plataforma</p>
    </div>

    <div v-if="loading" class="text-center py-4">
      <div class="spinner-border"></div>
    </div>
    <form v-else @submit.prevent="submit" style="max-width:700px">
      <div class="mb-3">
        <label class="form-label">Nombre del curso <span class="text-danger">*</span></label>
        <input v-model="form.nombre" type="text" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Descripción</label>
        <textarea v-model="form.descripcion" class="form-control" rows="4"></textarea>
      </div>
      <div class="mb-3">
        <label class="form-label">Profesor <span class="text-danger">*</span></label>
        <select v-model="form.idProfesor" class="form-select" required>
          <option value="">Seleccione un profesor</option>
          <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">{{ p.nombre }} {{ p.apellidos }}</option>
        </select>
      </div>
      <div class="row mb-3">
        <div class="col-md-6">
          <label class="form-label">Fecha de inicio <span class="text-danger">*</span></label>
          <input v-model="form.fechaInicio" type="date" class="form-control" required />
        </div>
        <div class="col-md-6">
          <label class="form-label">Fecha de fin <span class="text-danger">*</span></label>
          <input v-model="form.fechaFin" type="date" class="form-control" required />
        </div>
      </div>
      <div class="mb-3">
        <label class="form-label">Estado <span class="text-danger">*</span></label>
        <select v-model="form.estado" class="form-select" required>
          <option v-for="e in ESTADOS" :key="e">{{ e }}</option>
        </select>
      </div>
      <div class="mb-4">
        <FileUploader label="Imagen de portada" accept="image/*" @change="f => coverFile = f" />
      </div>
      <div class="d-flex justify-content-between">
        <RouterLink to="/admin" class="btn btn-outline-secondary">
          <i class="fas fa-arrow-left me-1"></i> Volver
        </RouterLink>
        <button type="submit" class="btn btn-primary" :disabled="saving">
          <i class="fas fa-save me-1"></i> Crear Curso
        </button>
      </div>
    </form>
  </main>
</template>
