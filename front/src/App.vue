<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from './stores/auth'
import AppNavBar from './components/AppNavBar.vue'

const auth = useAuthStore()

onMounted(async () => {
  if (!auth.user && auth.accessToken) {
    await auth.silentRefresh()
  }
})
</script>

<template>
  <div v-if="auth.isLoading" class="fullscreen-loader">
    <div class="spinner-border text-light" role="status">
      <span class="visually-hidden">Cargando...</span>
    </div>
  </div>
  <template v-else>
    <AppNavBar />
    <main role="main" class="pb-3">
      <RouterView />
    </main>
    <footer class="border-top footer text-muted">
      <div class="container">
        &copy; 2025 - MyVirtualAcademy
      </div>
    </footer>
  </template>
</template>

<style scoped>
.fullscreen-loader {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}
.footer {
  padding: 15px 0;
}
</style>
