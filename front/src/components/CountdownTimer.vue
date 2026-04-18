<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'

const props = defineProps<{ deadline: string }>()

const remaining = ref(0)
let timer: ReturnType<typeof setInterval>

function calcRemaining() {
  remaining.value = new Date(props.deadline).getTime() - Date.now()
}

const expired = computed(() => remaining.value <= 0)

const display = computed(() => {
  if (expired.value) return 'Plazo finalizado'
  const ms = remaining.value
  const d = Math.floor(ms / 86400000)
  const h = Math.floor((ms % 86400000) / 3600000)
  const m = Math.floor((ms % 3600000) / 60000)
  const s = Math.floor((ms % 60000) / 1000)
  return [d > 0 ? `${d}d` : '', `${h}h`, `${m}m`, `${s}s`].filter(Boolean).join(' ')
})

onMounted(() => {
  calcRemaining()
  timer = setInterval(() => {
    calcRemaining()
    if (expired.value) clearInterval(timer)
  }, 1000)
})

onUnmounted(() => clearInterval(timer))
</script>

<template>
  <span>{{ display }}</span>
</template>
