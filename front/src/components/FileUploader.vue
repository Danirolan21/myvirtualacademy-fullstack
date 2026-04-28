<script setup lang="ts">
import { ref } from 'vue'

defineProps<{ label?: string; accept?: string }>()
const emit = defineEmits<{ (e: 'change', file: File | null): void }>()

const error = ref('')

function handleChange(ev: Event) {
  const file = (ev.target as HTMLInputElement).files?.[0] ?? null
  if (file && file.size > 10 * 1024 * 1024) {
    error.value = 'El archivo supera el límite de 10 MB.'
    emit('change', null)
    return
  }
  error.value = ''
  emit('change', file)
}
</script>

<template>
  <div class="file-uploader">
    <label class="form-label">{{ label ?? 'Archivo' }}</label>
    <input type="file" class="form-control file-input" :accept="accept" @change="handleChange" />
    <p v-if="error" class="file-error">{{ error }}</p>
    <p class="form-text">Tamaño máximo: 10 MB</p>
  </div>
</template>

<style scoped>
.file-uploader { display: flex; flex-direction: column; gap: var(--sp-1); }
.file-input { padding: var(--sp-2); cursor: pointer; }
.file-error { font-size: var(--font-size-xs); color: var(--color-danger); margin: 0; }
</style>
