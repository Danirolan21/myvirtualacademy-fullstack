<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { formatDate } from '../../utils/format'
import { getCoursesByProfessor } from '../../api/courses'
import { getSubjectsByProfessor } from '../../api/subjects'
import type { VistaCursosDetalles, VistaAsignaturasProfesor } from '../../types'

const auth = useAuthStore()
const activeTab = ref<'cursos' | 'asignaturas'>('cursos')
const cursos = ref<VistaCursosDetalles[]>([])
const asignaturas = ref<VistaAsignaturasProfesor[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const [c, a] = await Promise.all([
      getCoursesByProfessor(auth.user!.id),
      getSubjectsByProfessor(auth.user!.id),
    ])
    cursos.value = c.data
    asignaturas.value = a.data
    if (!cursos.value.length && asignaturas.value.length) activeTab.value = 'asignaturas'
  } finally {
    loading.value = false
  }
})

const statusClass: Record<string, string> = {
  Activo: 'badge-activo', Borrador: 'badge-borrador',
  Finalizado: 'badge-finalizado', Archivado: 'badge-archivado', Suspendido: 'badge-suspendido',
}
</script>

<template>
  <div class="page">
    <div class="container">

      <!-- Tabs -->
      <div class="tabs-bar">
        <button class="tab-btn" :class="{ active: activeTab === 'cursos' }" @click="activeTab = 'cursos'">Mis Cursos</button>
        <button class="tab-btn" :class="{ active: activeTab === 'asignaturas' }" @click="activeTab = 'asignaturas'">Mis Asignaturas</button>
      </div>

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <!-- Tab Cursos -->
      <div v-else-if="activeTab === 'cursos'">
        <div v-if="cursos.length" class="cards-grid">
          <div class="course-card" v-for="curso in cursos" :key="curso.idCurso">
            <div class="card-cover" :style="{ backgroundImage: `url('/assets/images/courses/${curso.imagenPortada}')` }">
              <span class="badge" :class="statusClass[curso.estado] ?? 'badge-secondary'">{{ curso.estado }}</span>
            </div>
            <div class="card-body">
              <h3 class="card-title">{{ curso.nombreCurso }}</h3>
              <p class="card-meta"><i class="fas fa-user-tie"></i> {{ curso.nombreProfesor }}</p>
              <p class="card-meta"><i class="fas fa-calendar-alt"></i> {{ formatDate(curso.fechaInicio) }} – {{ formatDate(curso.fechaFin) }}</p>
              <div class="card-stats">
                <div class="stat"><span class="stat-val">{{ curso.numeroAsignaturas }}</span><span class="stat-lbl">Asignaturas</span></div>
                <div class="stat"><span class="stat-val">{{ curso.numeroAlumnos }}</span><span class="stat-lbl">Alumnos</span></div>
              </div>
              <RouterLink :to="`/admin/cursos/${curso.idCurso}`" class="btn btn-primary btn-sm">
                <i class="fas fa-eye"></i> Ver curso
              </RouterLink>
            </div>
          </div>
        </div>
        <div v-else class="empty-state">
          <i class="fas fa-graduation-cap"></i>
          <p>No eres tutor o suplente de ningún curso.</p>
        </div>
      </div>

      <!-- Tab Asignaturas -->
      <div v-else>
        <div v-if="asignaturas.length" class="cards-grid">
          <div class="course-card" v-for="asig in asignaturas" :key="asig.idAsignatura">
            <div class="card-cover card-cover-gradient"></div>
            <div class="card-body">
              <h3 class="card-title">{{ asig.nombreAsignatura }}</h3>
              <p class="card-meta"><i class="fas fa-book"></i> {{ asig.nombreCurso }}</p>
              <p class="card-meta"><i class="fas fa-calendar-alt"></i> {{ formatDate(asig.fechaInicio) }} – {{ formatDate(asig.fechaFin) }}</p>
              <div class="card-stats">
                <div class="stat"><span class="stat-val">{{ asig.numeroTemas }}</span><span class="stat-lbl">Temas</span></div>
                <div class="stat"><span class="stat-val">{{ asig.numeroContenidos }}</span><span class="stat-lbl">Contenidos</span></div>
              </div>
              <RouterLink :to="`/asignatura/${asig.idAsignatura}`" class="btn btn-primary btn-sm">
                <i class="fas fa-eye"></i> Ver asignatura
              </RouterLink>
            </div>
          </div>
        </div>
        <div v-else class="empty-state">
          <i class="fas fa-book-open"></i>
          <p>No eres profesor de ninguna asignatura.</p>
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
.tabs-bar {
  display: flex;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: var(--sp-6);
}
.tab-btn {
  padding: var(--sp-3) var(--sp-5);
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-muted);
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;
}
.tab-btn:hover { color: var(--color-text); }
.tab-btn.active { color: var(--color-text); border-bottom-color: var(--color-black); font-weight: var(--font-weight-semibold); }

.cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--sp-6);
}

.course-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  transition: transform 0.2s, box-shadow 0.2s;
  display: flex;
  flex-direction: column;
}
.course-card:hover { transform: translateY(-3px); box-shadow: var(--shadow-hover); }

.card-cover {
  height: 140px;
  background-size: cover;
  background-position: center;
  background-color: #e9ecef;
  position: relative;
}
.card-cover .badge { position: absolute; top: 8px; right: 8px; }
.card-cover-gradient { background: linear-gradient(135deg, #3498db, #2c3e50); }

.card-body { padding: var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-2); flex: 1; }
.card-body > .btn { margin-top: auto; }
.card-title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); color: var(--color-text); margin: 0; }
.card-meta { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; display: flex; align-items: center; gap: var(--sp-2); }
.card-meta i { width: 14px; text-align: center; }

.card-stats {
  display: flex;
  justify-content: space-around;
  border-top: 1px solid var(--color-border);
  padding-top: var(--sp-3);
  margin-top: var(--sp-1);
}
.stat { text-align: center; }
.stat-val { display: block; font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-primary); }
.stat-lbl { font-size: var(--font-size-xs); color: var(--color-muted); }

.empty-state {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-3); padding: var(--sp-12) 0; color: var(--color-muted); text-align: center;
}
.empty-state i { font-size: 2.5rem; opacity: 0.35; }
.empty-state p { margin: 0; }
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
</style>
