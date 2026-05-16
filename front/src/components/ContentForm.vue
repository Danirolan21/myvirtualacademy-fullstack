<script setup lang="ts">
import { ref, computed } from 'vue'
import { createContent } from '../api/content'
import FileUploader from './FileUploader.vue'

const props = defineProps<{ idTema: number; idAsignatura: number; nextOrden?: number }>()
const emit = defineEmits<{
  (e: 'saved', payload: { idContenido: number; tipo: string }): void
  (e: 'cancel'): void
}>()

const form = ref({
  titulo: '',
  descripcion: '',
  tipo: 'Video',
  orden: props.nextOrden ?? 1,
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
    const res = await createContent(fd)
    emit('saved', { idContenido: res.data.idContenido, tipo: form.value.tipo })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="content-form">
    <h5 class="form-title">Añadir nuevo contenido</h5>
    <form @submit.prevent="submit">

      <div class="field-row">
        <div class="field field-grow">
          <label class="form-label">Título</label>
          <input v-model="form.titulo" type="text" class="form-control" required />
        </div>
        <div class="field field-narrow">
          <label class="form-label">Orden</label>
          <input v-model.number="form.orden" type="number" class="form-control" min="1" required />
        </div>
      </div>

      <div class="field">
        <label class="form-label">Descripción</label>
        <textarea v-model="form.descripcion" class="form-control" rows="2" required></textarea>
      </div>

      <div class="field-row">
        <div class="field">
          <label class="form-label">Tipo de contenido</label>
          <select v-model="form.tipo" class="form-select" required>
            <option>Video</option>
            <option>Documento</option>
            <option>Enlace</option>
            <option>Tarea</option>
            <option>Examen</option>
          </select>
        </div>
        <div class="field" v-if="esEnlace">
          <label class="form-label">URL</label>
          <input v-model="form.urlContenido" type="url" class="form-control" placeholder="https://..." />
        </div>
        <div class="field" v-else>
          <FileUploader label="Archivo" @change="f => file = f" />
        </div>
      </div>

      <div class="field-row" v-if="esTareaExamen">
        <div class="field">
          <label class="form-label">Fecha de entrega</label>
          <input v-model="form.fechaEntrega" type="datetime-local" class="form-control" required />
        </div>
        <div class="field">
          <label class="form-label">Puntuación máxima</label>
          <input v-model.number="form.puntuacionMaxima" type="number" class="form-control" min="0" step="0.5" required />
        </div>
      </div>

      <div class="form-actions">
        <button type="button" class="btn btn-outline-secondary" @click="emit('cancel')">Cancelar</button>
        <button type="submit" class="btn btn-primary" :disabled="submitting">
          <span v-if="submitting"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
          <span v-else>Guardar contenido</span>
        </button>
      </div>

    </form>
  </div>
</template>

<style scoped>
.content-form {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-5);
}
.form-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  text-align: center;
  margin: 0 0 var(--sp-5);
  color: var(--color-text);
}
.field {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
  flex: 1;
  margin-bottom: var(--sp-4);
}
.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-4);
}
.field-grow { flex: 3; }
.field-narrow { flex: 1; }
.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--sp-3);
  padding-top: var(--sp-2);
}
</style>
