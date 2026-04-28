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
    <div class="spinner" role="status">
      <span class="visually-hidden">Cargando...</span>
    </div>
  </div>
  <template v-else>
    <div class="app-shell">
      <AppNavBar />
      <main class="app-main">
        <RouterView />
      </main>
      <footer class="site-footer">
        <div class="container">
          &copy; 2025 - MyVirtualAcademy - <a href="#">Privacy</a>
        </div>
      </footer>
    </div>
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
.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
.app-main {
  flex: 1;
}
.site-footer {
  margin-top: auto;
  padding: var(--sp-4) 0;
  border-top: 1px solid var(--color-border);
  color: var(--color-muted);
  font-size: var(--font-size-sm);
  background: var(--color-surface);
}
</style>
