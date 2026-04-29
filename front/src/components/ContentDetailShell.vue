<script setup lang="ts">
import { useRouter } from 'vue-router'
import { formatDateTime } from '../utils/format'

defineProps<{
  loading: boolean
  accessDenied: boolean
  titulo?: string
  badgeLabel: string
  iconClass: string
  iconWrapClass: string
  fechaPublicacion?: string
  breadcrumbSubjectId?: number
  breadcrumbSubjectName?: string
}>()

const router = useRouter()
</script>

<template>
  <div class="page">
    <div class="content-container">
      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <div v-else-if="accessDenied" class="access-denied">
        <i class="fas fa-lock access-denied-icon"></i>
        <h2>Acceso restringido</h2>
        <p>No estás inscrito en el curso al que pertenece este contenido.</p>
        <button class="btn btn-outline-secondary" @click="router.back()">
          <i class="fas fa-arrow-left"></i> Volver
        </button>
      </div>

      <template v-else-if="titulo != null">
        <nav class="breadcrumb">
          <RouterLink to="/" class="breadcrumb-item">Inicio</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <RouterLink
            v-if="breadcrumbSubjectId"
            :to="`/asignatura/${breadcrumbSubjectId}`"
            class="breadcrumb-item"
          >{{ breadcrumbSubjectName }}</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <span class="breadcrumb-item breadcrumb-active">{{ titulo }}</span>
        </nav>

        <div class="content-card">
          <div class="content-header">
            <div class="content-header-left">
              <div class="content-icon-wrap" :class="iconWrapClass">
                <i :class="iconClass"></i>
              </div>
              <h1 class="content-title">{{ titulo }}</h1>
            </div>
            <span class="badge badge-secondary">{{ badgeLabel }}</span>
          </div>

          <div class="content-body">
            <p v-if="fechaPublicacion" class="pub-date">
              <i class="fas fa-calendar-alt"></i> Publicado el {{ formatDateTime(fechaPublicacion) }}
            </p>
            <slot />
          </div>

          <div class="content-footer">
            <button class="btn btn-outline-secondary" @click="router.back()">
              <i class="fas fa-arrow-left"></i> Volver
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.content-container { max-width: 800px; margin: 0 auto; padding: var(--sp-6) var(--sp-4); }
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

.breadcrumb { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; margin-bottom: var(--sp-4); font-size: var(--font-size-sm); }
.breadcrumb-item { color: var(--color-muted); text-decoration: none; transition: color 0.15s; }
.breadcrumb-item:hover { color: var(--color-text); }
.breadcrumb-active { color: var(--color-text); font-weight: var(--font-weight-medium); }
.breadcrumb-sep { color: var(--color-muted); font-size: 10px; }

.content-card { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; box-shadow: var(--shadow-sm); }

.content-header {
  padding: var(--sp-5) var(--sp-6);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-4);
}
.content-header-left { display: flex; align-items: center; gap: var(--sp-4); flex: 1; min-width: 0; }
.content-icon-wrap {
  width: 44px; height: 44px;
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-xl);
  flex-shrink: 0;
}
.content-title { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text); margin: 0; }

.content-body { padding: var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-5); }
.content-footer { padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); background: var(--color-muted-bg); }

.pub-date { font-size: var(--font-size-sm); color: var(--color-muted); display: flex; align-items: center; gap: var(--sp-2); margin: 0; }

.access-denied {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  text-align: center; padding: var(--sp-16) var(--sp-4); color: var(--color-muted);
}
.access-denied-icon { font-size: 3rem; color: var(--color-danger); opacity: 0.6; margin-bottom: var(--sp-4); }
.access-denied h2 { color: var(--color-text); margin: 0 0 var(--sp-2); }
.access-denied p { margin: 0 0 var(--sp-6); }
</style>
