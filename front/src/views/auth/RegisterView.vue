<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

onMounted(() => {
  router.replace({ path: '/admin', query: { tab: 'usuarios' } })
})
</script>

<template>
  <div class="page">
    <div class="loading-center"><div class="spinner"></div></div>
  </div>
</template>

<style scoped>
.loading-center { display: flex; justify-content: center; padding: 4rem 0; }
</style>
