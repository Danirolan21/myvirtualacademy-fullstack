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
  <div class="page">
    <div class="container">
      <div class="form-page">

        <div class="form-page-header">
          <RouterLink to="/admin" class="back-link">
            <i class="fas fa-arrow-left"></i> Volver
          </RouterLink>
          <h1 class="form-page-title">Crear Nuevo Curso</h1>
          <p class="form-page-sub">Complete el formulario para crear un nuevo curso en la plataforma.</p>
        </div>

        <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

        <form v-else @submit.prevent="submit" class="form-body">
          <div class="field">
            <label class="form-label" for="cn-nombre">Nombre del curso <span class="req">*</span></label>
            <input id="cn-nombre" v-model="form.nombre" type="text" class="form-control" required />
          </div>

          <div class="field">
            <label class="form-label" for="cn-desc">Descripción</label>
            <textarea id="cn-desc" v-model="form.descripcion" class="form-control" rows="4"></textarea>
          </div>

          <div class="field">
            <label class="form-label" for="cn-prof">Profesor <span class="req">*</span></label>
            <select id="cn-prof" v-model="form.idProfesor" class="form-select" required>
              <option value="">Seleccione un profesor</option>
              <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">
                {{ p.nombre }} {{ p.apellidos }}
              </option>
            </select>
          </div>

          <div class="field-row">
            <div class="field">
              <label class="form-label" for="cn-inicio">Fecha de inicio <span class="req">*</span></label>
              <input id="cn-inicio" v-model="form.fechaInicio" type="date" class="form-control" required />
            </div>
            <div class="field">
              <label class="form-label" for="cn-fin">Fecha de fin <span class="req">*</span></label>
              <input id="cn-fin" v-model="form.fechaFin" type="date" class="form-control" required />
            </div>
          </div>

          <div class="field">
            <label class="form-label" for="cn-estado">Estado <span class="req">*</span></label>
            <select id="cn-estado" v-model="form.estado" class="form-select" required>
              <option v-for="e in ESTADOS" :key="e">{{ e }}</option>
            </select>
          </div>

          <div class="field">
            <FileUploader label="Imagen de portada" accept="image/*" @change="f => coverFile = f" />
          </div>

          <div class="form-actions">
            <RouterLink to="/admin" class="btn btn-outline-secondary">
              <i class="fas fa-arrow-left"></i> Volver
            </RouterLink>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              <span v-if="saving"><i class="fas fa-spinner fa-spin"></i> Creando…</span>
              <span v-else><i class="fas fa-save"></i> Crear Curso</span>
            </button>
          </div>
        </form>

      </div>
    </div>
  </div>
</template>

<style scoped>
.form-page { max-width: 700px; }
.form-page-header { margin-bottom: var(--sp-8); }
.back-link {
  display: inline-flex; align-items: center; gap: var(--sp-2);
  color: var(--color-muted); font-size: var(--font-size-sm);
  text-decoration: none; margin-bottom: var(--sp-4); transition: color 0.15s;
}
.back-link:hover { color: var(--color-text); }
.form-page-title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0 0 var(--sp-2); }
.form-page-sub { color: var(--color-muted); margin: 0; font-size: var(--font-size-sm); }
.form-body { display: flex; flex-direction: column; gap: var(--sp-4); }
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.req { color: var(--color-danger); }
.form-actions { display: flex; justify-content: space-between; padding-top: var(--sp-4); }
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
</style>
