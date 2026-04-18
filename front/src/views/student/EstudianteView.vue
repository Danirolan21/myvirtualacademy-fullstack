<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { getSubjectsByStudent } from '../../api/subjects'
import type { AsignaturaUsuarioDTO } from '../../types'

const auth = useAuthStore()
const asignaturas = ref<AsignaturaUsuarioDTO[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await getSubjectsByStudent(auth.user!.id)
    asignaturas.value = res.data
  } finally {
    loading.value = false
  }
})

function diasRestantes(fechaFin: string) {
  return Math.max(0, Math.floor((new Date(fechaFin).getTime() - Date.now()) / 86400000))
}
</script>

<template>
  <main class="container">
    <div class="tabs"><div class="tab active">Mis asignaturas</div></div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>

    <div v-else-if="asignaturas.length" class="courses-grid">
      <div class="course-card" v-for="asig in asignaturas" :key="asig.idAsignatura">
        <div class="course-image">
          <div class="course-image-bg"></div>
          <div class="course-book"></div>
        </div>
        <div class="course-content">
          <div class="d-flex align-items-center mb-2">
            <h3 class="course-title mb-0">{{ asig.nombreAsignatura }}</h3>
            <span class="course-status ms-auto" :class="asig.estado === 'Activo' ? 'status-active' : 'status-inactive'">{{ asig.estado }}</span>
          </div>
          <div class="course-info"><i class="fas fa-graduation-cap me-1"></i> {{ asig.nombreCurso }}</div>
          <div class="course-info"><i class="fas fa-user-tie me-1"></i> {{ asig.nombreProfesor }}</div>
          <div class="course-dates">
            <i class="far fa-calendar-alt me-1"></i>
            {{ new Date(asig.fechaInicio).toLocaleDateString('es-ES') }} - {{ new Date(asig.fechaFin).toLocaleDateString('es-ES') }}
          </div>
          <div class="course-progress mt-3">
            <div class="progress-label">
              <span>Progreso</span><span>{{ asig.progreso.toFixed(1) }}%</span>
            </div>
            <div class="progress-bar">
              <div class="progress-value" :style="{ width: `${asig.progreso}%` }"></div>
            </div>
          </div>
          <div class="course-stats">
            <div class="course-stat">
              <div class="course-stat-value">{{ asig.numeroTemas }}</div>
              <div class="course-stat-label">Temas</div>
            </div>
            <div class="course-stat">
              <div class="course-stat-value">{{ diasRestantes(asig.fechaFin) }}</div>
              <div class="course-stat-label">Días restantes</div>
            </div>
          </div>
          <div class="course-actions">
            <RouterLink :to="`/asignatura/${asig.idAsignatura}`" class="btn btn-primary btn-sm">
              Ver asignatura <i class="fas fa-arrow-right ms-1"></i>
            </RouterLink>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <i class="fas fa-info-circle fa-2x mb-3"></i>
      <h3>No tienes asignaturas asignadas</h3>
      <p>Contacta con tu administrador si crees que esto es un error.</p>
    </div>
  </main>
</template>

<style scoped>
.tabs { display: flex; border-bottom: 2px solid #e2e8f0; margin-bottom: 1.5rem; padding-top: 1rem; }
.tab { padding: 0.75rem 1.25rem; font-weight: 500; border-bottom: 2px solid #2563eb; color: #2563eb; margin-bottom: -2px; }
.courses-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.5rem; margin-bottom: 2rem; }
.course-card { background: white; border-radius: 0.5rem; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); transition: transform 0.2s, box-shadow 0.2s; }
.course-card:hover { transform: translateY(-5px); box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1); }
.course-image { height: 140px; position: relative; overflow: hidden; }
.course-image-bg { position: absolute; width: 100%; height: 100%; background: linear-gradient(135deg,#3498db,#2c3e50); }
.course-book { position: absolute; top: 50%; left: 50%; transform: translate(-50%,-50%); width: 40px; height: 50px; background: rgba(255,255,255,0.8); border-radius: 3px; }
.course-content { padding: 1.25rem; }
.course-title { font-size: 1.1rem; font-weight: 600; color: #1e293b; }
.course-info, .course-dates { display: flex; align-items: center; gap: 0.5rem; color: #64748b; font-size: 0.875rem; margin-bottom: 0.25rem; }
.course-status { padding: 0.25rem 0.5rem; border-radius: 0.25rem; font-size: 0.75rem; font-weight: 500; }
.status-active { background: rgba(34,197,94,0.1); color: #22c55e; }
.status-inactive { background: rgba(239,68,68,0.1); color: #ef4444; }
.progress-label { display: flex; justify-content: space-between; font-size: 0.875rem; margin-bottom: 4px; }
.progress-bar { height: 8px; background: #e2e8f0; border-radius: 4px; overflow: hidden; }
.progress-value { height: 100%; background: #2563eb; border-radius: 4px; }
.course-stats { display: flex; justify-content: space-between; margin-top: 1rem; padding-top: 0.5rem; border-top: 1px solid #e2e8f0; }
.course-stat { text-align: center; }
.course-stat-value { font-weight: 600; color: #2563eb; }
.course-stat-label { font-size: 0.75rem; color: #64748b; }
.course-actions { margin-top: 1rem; text-align: right; }
.empty-state { text-align: center; padding: 3rem 1rem; background: rgba(255,193,7,0.1); border-radius: 0.5rem; margin-top: 1rem; color: #856404; }
</style>
