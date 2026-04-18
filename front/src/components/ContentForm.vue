<script setup lang="ts">
import { ref, computed } from 'vue'
import { createContent } from '../api/content'
import FileUploader from './FileUploader.vue'

const props = defineProps<{ idTema: number; idAsignatura: number }>()
const emit = defineEmits<{ (e: 'saved'): void; (e: 'cancel'): void }>()

const form = ref({
  titulo: '',
  descripcion: '',
  tipo: 'Video',
  orden: 1,
  urlContenido: '',
  fechaEntrega: '',
  puntuacionMaxima: 10,
})
const file = ref<File | null>(null)
const submitting = ref(false)

const esEnlace = computed(() => form.value.tipo === 'Enlace')
const esTareaExamen = computed(() => ['Tarea', 'Quiz', 'Examen'].includes(form.value.tipo))

async function submit() {
  submitting.value = true
  try {
    const fd = new FormData()
    fd.append('idTema', String(props.idTema))
    fd.append('idAsignatura', String(props.idAsignatura))
    fd.append('titulo', form.value.titulo)
    fd.append('descripcion', form.value.descripcion)
    fd.append('tipo', form.value.tipo)
    fd.append('orden', String(form.value.orden))
    if (esEnlace.value) fd.append('urlContenido', form.value.urlContenido)
    if (esTareaExamen.value) {
      fd.append('fechaEntrega', form.value.fechaEntrega)
      fd.append('puntuacionMaxima', String(form.value.puntuacionMaxima))
    }
    if (file.value) fd.append('archivoContenido', file.value)
    await createContent(fd)
    emit('saved')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="add-content-form p-3 border rounded">
    <h5 class="mb-3">Añadir nuevo contenido</h5>
    <form @submit.prevent="submit">
      <div class="row mb-3">
        <div class="col-md-8">
          <label class="form-label">Título</label>
          <input v-model="form.titulo" type="text" class="form-control" required />
        </div>
        <div class="col-md-4">
          <label class="form-label">Orden</label>
          <input v-model.number="form.orden" type="number" class="form-control" min="1" required />
        </div>
      </div>
      <div class="mb-3">
        <label class="form-label">Descripción</label>
        <textarea v-model="form.descripcion" class="form-control" rows="2" required></textarea>
      </div>
      <div class="row mb-3">
        <div class="col-md-6">
          <label class="form-label">Tipo</label>
          <select v-model="form.tipo" class="form-select" required>
            <option>Video</option>
            <option>Documento</option>
            <option>Enlace</option>
            <option>Tarea</option>
            <option>Examen</option>
          </select>
        </div>
        <div class="col-md-6" v-if="esEnlace">
          <label class="form-label">URL</label>
          <input v-model="form.urlContenido" type="url" class="form-control" placeholder="https://..." />
        </div>
        <div class="col-md-6" v-else>
          <FileUploader label="Archivo" @change="f => file = f" />
        </div>
      </div>
      <div class="row mb-3" v-if="esTareaExamen">
        <div class="col-md-6">
          <label class="form-label">Fecha de entrega</label>
          <input v-model="form.fechaEntrega" type="datetime-local" class="form-control" required />
        </div>
        <div class="col-md-6">
          <label class="form-label">Puntuación máxima</label>
          <input v-model.number="form.puntuacionMaxima" type="number" class="form-control" min="0" step="0.5" required />
        </div>
      </div>
      <div class="d-flex gap-2 justify-content-end">
        <button type="button" class="btn btn-secondary" @click="emit('cancel')">Cancelar</button>
        <button type="submit" class="btn btn-primary" :disabled="submitting">Guardar</button>
      </div>
    </form>
  </div>
</template>
