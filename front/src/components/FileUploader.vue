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
  <div>
    <label class="form-label">{{ label ?? 'Archivo' }}</label>
    <input type="file" class="form-control" :accept="accept" @change="handleChange" />
    <div v-if="error" class="text-danger small mt-1">{{ error }}</div>
    <div class="form-text">Tamaño máximo: 10 MB</div>
  </div>
</template>
