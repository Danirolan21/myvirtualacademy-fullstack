<script setup lang="ts">
import type { VistaCursosDetalles } from '../types'
import { formatDate } from '../utils/format'

defineProps<{ curso: VistaCursosDetalles; adminMode?: boolean }>()
const emit = defineEmits<{ (e: 'options', id: number): void }>()

const statusClass: Record<string, string> = {
  Activo:    'status-activo',
  Borrador:  'status-borrador',
  Finalizado:'status-finalizado',
  Archivado: 'status-archivado',
  Suspendido:'status-suspendido',
}
</script>

<template>
  <article class="course-card">
    <RouterLink class="card-link" :to="`/admin/cursos/${curso.idCurso}`">

      <!-- Cover image -->
      <div
        class="course-cover"
        :style="{ backgroundImage: `url('/assets/images/courses/${curso.imagenPortada}')` }"
        role="img"
        :aria-label="`Portada de ${curso.nombreCurso}`"
      >
        <!-- 3-dot action button -->
        <button
          v-if="adminMode"
          class="btn-options"
          type="button"
          title="Opciones del curso"
          @click.prevent="emit('options', curso.idCurso)"
        >
          <i class="fas fa-ellipsis-v"></i>
        </button>

        <!-- Status badge -->
        <span class="status-badge" :class="statusClass[curso.estado]">
          {{ curso.estado }}
        </span>
      </div>

      <!-- Card body -->
      <div class="course-body">
        <h3 class="course-title">
          <i class="fas fa-sync-alt title-icon"></i>
          {{ curso.nombreCurso }}
        </h3>
        <ul class="course-meta">
          <li><i class="fas fa-user"></i><span>{{ curso.nombreProfesor }}</span></li>
          <li><i class="fas fa-calendar-alt"></i><span>Inicio: {{ formatDate(curso.fechaInicio) }}</span></li>
          <li><i class="fas fa-book-open"></i><span>Asignaturas: {{ curso.numeroAsignaturas }}</span></li>
          <li><i class="fas fa-users"></i><span>Alumnos: {{ curso.numeroAlumnos }}</span></li>
        </ul>
      </div>
    </RouterLink>
  </article>
</template>

<style scoped>
.course-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  transition: transform 0.2s, box-shadow 0.2s;
}
.course-card:hover {
  transform: translateY(-3px);
  box-shadow: var(--shadow-hover);
}

.card-link {
  display: block;
  text-decoration: none;
  color: inherit;
}
.card-link:hover { text-decoration: none; }

/* ── Cover image ── */
.course-cover {
  position: relative;
  height: 160px;
  background-size: cover;
  background-position: center;
  background-color: #e9ecef;
}

/* ── 3-dot button ── */
.btn-options {
  position: absolute;
  top: 8px;
  left: 8px;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.5);
  border: none;
  border-radius: var(--radius-sm);
  color: #fff;
  font-size: 0.9rem;
  cursor: pointer;
  transition: background 0.15s;
  z-index: 2;
}
.btn-options:hover { background: rgba(0, 0, 0, 0.75); }

/* ── Status badge ── */
.status-badge {
  position: absolute;
  top: 8px;
  right: 8px;
  padding: 3px 10px;
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  letter-spacing: 0.04em;
  z-index: 2;
}
.status-activo     { background: var(--badge-activo-bg);     color: var(--badge-activo-text); }
.status-borrador   { background: var(--badge-borrador-bg);   color: var(--badge-borrador-text); }
.status-finalizado { background: var(--badge-finalizado-bg); color: var(--badge-finalizado-text); }
.status-archivado  { background: var(--badge-archivado-bg);  color: var(--badge-archivado-text); }
.status-suspendido { background: var(--badge-suspendido-bg); color: var(--badge-suspendido-text); }

/* ── Card body ── */
.course-body {
  padding: var(--sp-4);
}

.course-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0 0 var(--sp-3);
  line-height: var(--line-height-tight);
  display: flex;
  align-items: flex-start;
  gap: var(--sp-2);
}
.title-icon {
  color: var(--color-danger);
  font-size: 0.9rem;
  flex-shrink: 0;
  margin-top: 3px;
}

.course-meta {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}
.course-meta li {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
}
.course-meta li i {
  width: 14px;
  text-align: center;
  font-size: 0.8rem;
  flex-shrink: 0;
}
</style>
