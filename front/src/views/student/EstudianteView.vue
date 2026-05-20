<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { formatDate } from '../../utils/format'
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

const GRADIENT_PAIRS = [
  ['#6366f1', '#8b5cf6'], // indigo → violeta
  ['#0ea5e9', '#06b6d4'], // sky → cyan
  ['#10b981', '#059669'], // emerald → green
  ['#f59e0b', '#ef4444'], // amber → red
  ['#ec4899', '#8b5cf6'], // pink → violet
  ['#14b8a6', '#0ea5e9'], // teal → sky
  ['#f97316', '#eab308'], // orange → yellow
  ['#6366f1', '#ec4899'], // indigo → pink
  ['#84cc16', '#10b981'], // lime → emerald
  ['#0f172a', '#1e40af'], // dark → blue
]

function coverGradient(idAsignatura: number): string {
  const [a, b] = GRADIENT_PAIRS[idAsignatura % GRADIENT_PAIRS.length]
  return `linear-gradient(135deg, ${a}, ${b})`
}
</script>

<template>
  <div class="page">
    <div class="container">

      <div class="tabs-bar">
        <button class="tab-btn active">Mis Asignaturas</button>
      </div>

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <div v-else-if="asignaturas.length" class="cards-grid">
        <div class="subject-card" v-for="asig in asignaturas" :key="asig.idAsignatura">

          <div class="card-cover" :style="{ background: coverGradient(asig.idAsignatura) }"></div>

          <div class="card-body">
            <div class="card-content">
              <div class="card-title-row">
                <h3 class="card-title">{{ asig.nombreAsignatura }}</h3>
                <span class="badge" :class="asig.estado === 'Activo' ? 'badge-activo' : 'badge-finalizado'">
                  {{ asig.estado }}
                </span>
              </div>

              <p class="card-meta"><i class="fas fa-graduation-cap"></i> {{ asig.nombreCurso }}</p>
              <p class="card-meta"><i class="fas fa-user-tie"></i> {{ asig.nombreProfesor }}</p>
              <p class="card-meta"><i class="far fa-calendar-alt"></i> {{ formatDate(asig.fechaInicio) }} – {{ formatDate(asig.fechaFin) }}</p>

              <div class="progress-section">
                <div class="progress-label">
                  <span>Progreso</span>
                  <span class="progress-value-text">{{ asig.progreso.toFixed(1) }}%</span>
                </div>
                <div class="progress-track">
                  <div class="progress-fill" :style="{ width: `${asig.progreso}%` }"></div>
                </div>
              </div>

              <div class="card-stats">
                <div class="stat">
                  <span class="stat-val">{{ asig.numeroTemas }}</span>
                  <span class="stat-lbl">Temas</span>
                </div>
                <div class="stat">
                  <span class="stat-val">{{ diasRestantes(asig.fechaFin) }}</span>
                  <span class="stat-lbl">Días rest.</span>
                </div>
              </div>
            </div>

            <RouterLink :to="`/asignatura/${asig.idAsignatura}`" class="btn btn-primary btn-sm btn-block">
              Ver asignatura <i class="fas fa-arrow-right"></i>
            </RouterLink>
          </div>

        </div>
      </div>

      <div v-else class="empty-state">
        <i class="fas fa-info-circle empty-icon"></i>
        <h3 class="empty-title">No tienes asignaturas asignadas</h3>
        <p class="empty-desc">Contacta con tu administrador si crees que esto es un error.</p>
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

@media (max-width: 768px) {
  .tabs-bar {
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
    -webkit-mask-image: linear-gradient(to right, black 88%, transparent);
            mask-image: linear-gradient(to right, black 88%, transparent);
  }
  .tabs-bar::-webkit-scrollbar { display: none; }
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
  white-space: nowrap;
  flex-shrink: 0;
}
.tab-btn.active {
  color: var(--color-text);
  border-bottom-color: var(--color-black);
  font-weight: var(--font-weight-semibold);
}

.cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--sp-6);
}

.subject-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  transition: transform 0.2s, box-shadow 0.2s;
  display: flex;
  flex-direction: column;
}
.subject-card:hover { transform: translateY(-3px); box-shadow: var(--shadow-hover); }

.card-cover {
  height: 140px;
}

.card-body {
  padding: var(--sp-4);
  display: flex;
  flex-direction: column;
  flex: 1;
}

.card-content {
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
  flex: 1;
}

.card-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--sp-2);
}
.card-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
  flex: 1;
}
.card-meta {
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  margin: 0;
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}
.card-meta i { width: 14px; text-align: center; }

.progress-section { margin-top: var(--sp-1); }
.progress-label {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-xs);
  color: var(--color-muted);
  margin-bottom: var(--sp-1);
}
.progress-value-text { font-weight: var(--font-weight-semibold); color: var(--color-primary); }
.progress-track {
  height: 8px;
  background: var(--color-muted-bg);
  border-radius: 999px;
  overflow: hidden;
}
.progress-fill {
  height: 100%;
  background: var(--color-primary);
  border-radius: 999px;
  transition: width 0.4s ease;
}

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

.btn-block { width: 100%; justify-content: center; margin-top: var(--sp-3); }

.empty-state {
  text-align: center;
  padding: var(--sp-12) var(--sp-4);
  color: var(--color-muted);
}
.empty-icon { font-size: 2.5rem; margin-bottom: var(--sp-4); display: block; }
.empty-title { font-size: var(--font-size-xl); font-weight: var(--font-weight-semibold); margin: 0 0 var(--sp-2); color: var(--color-text); }
.empty-desc { font-size: var(--font-size-sm); margin: 0; }

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
</style>
