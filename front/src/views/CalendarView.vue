<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { getCalendarEvents, type CalendarEvent } from '../api/calendar'

const today = new Date()
const currentYear = ref(today.getFullYear())

const MONTHS = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'
]

const selectedDay = ref<{ year: number; month: number; day: number } | null>(null)
const events = ref<CalendarEvent[]>([])
const loading = ref(true)

// Colores deterministas por idCurso
const PALETTE = [
  '#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#ec4899',
  '#14b8a6', '#f97316', '#8b5cf6', '#84cc16', '#ef4444',
]
function courseColor(idCurso: number): string {
  return PALETTE[idCurso % PALETTE.length]
}

// IDs únicos de cursos presentes en eventos
const courseIds = computed(() => [...new Set(events.value.map(e => e.idCurso))])

// Filtro de cursos visibles (todos por defecto)
const visibleCourses = ref<Set<number>>(new Set())
onMounted(async () => {
  try {
    const res = await getCalendarEvents()
    events.value = res.data
    visibleCourses.value = new Set(res.data.map(e => e.idCurso))
  } finally {
    loading.value = false
  }
})

function toggleCourse(id: number) {
  const next = new Set(visibleCourses.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  visibleCourses.value = next
}

// Eventos filtrados
const filteredEvents = computed(() =>
  events.value.filter(e => visibleCourses.value.has(e.idCurso))
)

// Eventos por fecha (yyyy-MM-dd)
const eventsByDate = computed(() => {
  const map = new Map<string, CalendarEvent[]>()
  for (const ev of filteredEvents.value) {
    const list = map.get(ev.fecha) ?? []
    list.push(ev)
    map.set(ev.fecha, list)
  }
  return map
})

function eventsForDay(year: number, month: number, day: number): CalendarEvent[] {
  const key = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
  return eventsByDate.value.get(key) ?? []
}

// Popover
const popover = ref<{ year: number; month: number; day: number; events: CalendarEvent[] } | null>(null)

function selectDay(year: number, month: number, day: number | null) {
  if (!day) return
  const dayEvents = eventsForDay(year, month, day)
  if (
    popover.value?.year === year &&
    popover.value?.month === month &&
    popover.value?.day === day
  ) {
    popover.value = null
    return
  }
  if (dayEvents.length) {
    popover.value = { year, month, day, events: dayEvents }
  } else {
    popover.value = null
    // toggle selection
    if (selectedDay.value?.year === year && selectedDay.value?.month === month && selectedDay.value?.day === day) {
      selectedDay.value = null
    } else {
      selectedDay.value = { year, month, day }
    }
  }
}

interface CalDay {
  day: number | null
  isToday: boolean
  isWeekend: boolean
  isSelected: boolean
}

function buildMonth(year: number, month: number): CalDay[][] {
  const firstDow = new Date(year, month, 1).getDay()
  const offset = (firstDow + 6) % 7
  const daysInMonth = new Date(year, month + 1, 0).getDate()

  const cells: (CalDay | null)[] = []
  for (let i = 0; i < offset; i++) cells.push(null)
  for (let d = 1; d <= daysInMonth; d++) {
    const dow = (offset + d - 1) % 7
    cells.push({
      day: d,
      isToday: year === today.getFullYear() && month === today.getMonth() && d === today.getDate(),
      isWeekend: dow >= 5,
      isSelected: !!(selectedDay.value && selectedDay.value.year === year && selectedDay.value.month === month && selectedDay.value.day === d),
    })
  }
  while (cells.length % 7 !== 0) cells.push(null)
  const weeks: CalDay[][] = []
  for (let i = 0; i < cells.length; i += 7) {
    weeks.push(cells.slice(i, i + 7).map(c => c ?? { day: null, isToday: false, isWeekend: false, isSelected: false }))
  }
  return weeks
}

const months = computed(() =>
  MONTHS.map((name, idx) => ({
    name,
    index: idx,
    weeks: buildMonth(currentYear.value, idx),
  }))
)

function eventTypeIcon(tipo: string) {
  if (tipo === 'entrega') return 'fas fa-tasks'
  if (tipo === 'inicio_curso') return 'fas fa-play-circle'
  return 'fas fa-flag-checkered'
}

function eventTypeLabel(tipo: string) {
  if (tipo === 'entrega') return 'Entrega'
  if (tipo === 'inicio_curso') return 'Inicio de curso'
  return 'Fin de curso'
}

// Nombre del curso por idCurso (para el filtro)
function courseName(idCurso: number): string {
  const ev = events.value.find(e => e.idCurso === idCurso && e.tipo === 'inicio_curso')
  return ev?.titulo ?? `Curso ${idCurso}`
}
</script>

<template>
  <div class="page">
    <div class="cal-container">

      <!-- Year navigation -->
      <div class="year-nav">
        <button class="year-btn" @click="currentYear--"><i class="fas fa-chevron-left"></i></button>
        <span class="year-label">{{ currentYear }}</span>
        <button class="year-btn" @click="currentYear++"><i class="fas fa-chevron-right"></i></button>
      </div>

      <!-- Course filter (solo si hay más de 1 curso) -->
      <div v-if="courseIds.length > 1" class="course-filter">
        <button
          v-for="id in courseIds"
          :key="id"
          class="course-chip"
          :class="{ 'course-chip-off': !visibleCourses.has(id) }"
          :style="{ '--chip-color': courseColor(id) }"
          @click="toggleCourse(id)"
        >
          <span class="chip-dot" :style="{ background: courseColor(id) }"></span>
          {{ courseName(id) }}
        </button>
      </div>

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <!-- 4×3 grid of months -->
      <div v-else class="months-grid">
        <div class="month-card" v-for="m in months" :key="m.index">
          <div class="month-name">{{ m.name }}</div>

          <div class="weekdays-row">
            <span v-for="wd in ['Lu','Ma','Mi','Ju','Vi','Sa','Do']" :key="wd" class="wd-cell">{{ wd }}</span>
          </div>

          <div class="week-row" v-for="(week, wi) in m.weeks" :key="wi">
            <div
              v-for="(cell, ci) in week"
              :key="ci"
              class="day-cell-wrap"
            >
              <button
                class="day-cell"
                :class="{
                  'day-empty': !cell.day,
                  'day-today': cell.isToday,
                  'day-weekend': cell.isWeekend && !cell.isToday,
                  'day-selected': cell.isSelected && !cell.isToday,
                  'day-has-events': cell.day && eventsForDay(currentYear, m.index, cell.day!).length > 0,
                }"
                :disabled="!cell.day"
                @click="selectDay(currentYear, m.index, cell.day)"
              >{{ cell.day ?? '' }}</button>

              <!-- Event dots -->
              <div v-if="cell.day" class="event-dots">
                <span
                  v-for="(ev, ei) in eventsForDay(currentYear, m.index, cell.day!).slice(0, 3)"
                  :key="ei"
                  class="event-dot"
                  :style="{ background: courseColor(ev.idCurso) }"
                ></span>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Popover -->
    <Teleport to="body">
      <div v-if="popover" class="popover-overlay" @click.self="popover = null">
        <div class="popover-box">
          <div class="popover-header">
            <span class="popover-date">
              {{ popover.day }} de {{ MONTHS[popover.month] }} {{ popover.year }}
            </span>
            <button class="popover-close" @click="popover = null"><i class="fas fa-times"></i></button>
          </div>
          <div class="popover-events">
            <div v-for="(ev, i) in popover.events" :key="i" class="popover-event">
              <div class="popover-event-dot" :style="{ background: courseColor(ev.idCurso) }"></div>
              <div class="popover-event-body">
                <div class="popover-event-type">
                  <i :class="eventTypeIcon(ev.tipo)"></i> {{ eventTypeLabel(ev.tipo) }}
                </div>
                <div class="popover-event-title" :style="{ color: courseColor(ev.idCurso) }">{{ ev.titulo }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.cal-container {
  max-width: 1100px;
  margin: 0 auto;
  padding: var(--sp-6) var(--sp-4) var(--sp-10);
}

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

/* Year nav */
.year-nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--sp-5);
  margin-bottom: var(--sp-5);
}
.year-label {
  font-size: 1.75rem;
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  min-width: 5rem;
  text-align: center;
}
.year-btn {
  width: 36px; height: 36px;
  border-radius: 50%;
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-sm);
  transition: background 0.15s, border-color 0.15s;
}
.year-btn:hover { background: var(--color-muted-bg); border-color: var(--color-primary); color: var(--color-primary); }

/* Course filter */
.course-filter {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-2);
  justify-content: center;
  margin-bottom: var(--sp-6);
}
.course-chip {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-1) var(--sp-3);
  border: 1.5px solid var(--chip-color, var(--color-border));
  border-radius: 999px;
  background: none;
  font-family: var(--font-sans);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  cursor: pointer;
  transition: opacity 0.15s, background 0.15s;
}
.course-chip-off { opacity: 0.35; }
.chip-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }

/* Months grid */
.months-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--sp-5);
}

/* Month card */
.month-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-4);
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
}

.month-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  text-align: center;
  margin-bottom: var(--sp-3);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

/* Weekday headers */
.weekdays-row { display: grid; grid-template-columns: repeat(7, 1fr); margin-bottom: var(--sp-1); }
.wd-cell { font-size: 0.65rem; font-weight: var(--font-weight-semibold); color: var(--color-muted); text-align: center; padding: 2px 0; }

/* Day rows */
.week-row { display: grid; grid-template-columns: repeat(7, 1fr); }

.day-cell-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.day-cell {
  font-size: 0.72rem;
  text-align: center;
  padding: 3px 1px;
  border: none;
  background: none;
  border-radius: 50%;
  cursor: pointer;
  color: var(--color-text);
  font-family: var(--font-sans);
  transition: background 0.1s, color 0.1s;
  line-height: 1.6;
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
}
.day-cell:hover:not(.day-empty):not(.day-today) { background: var(--color-muted-bg); }
.day-cell:disabled { cursor: default; }
.day-empty { pointer-events: none; }
.day-weekend { color: var(--color-muted); }
.day-today { background: #0d6efd; color: white; font-weight: var(--font-weight-bold); }
.day-selected { background: #0b5ed7; color: white; font-weight: var(--font-weight-semibold); }
.day-has-events:not(.day-today):not(.day-selected) { font-weight: var(--font-weight-semibold); }

/* Event dots */
.event-dots {
  display: flex;
  gap: 2px;
  justify-content: center;
  min-height: 5px;
  margin-top: 1px;
}
.event-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  flex-shrink: 0;
}

/* Popover */
.popover-overlay {
  position: fixed;
  inset: 0;
  z-index: 200;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0,0,0,0.15);
}
.popover-box {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: 0 8px 32px rgba(0,0,0,0.12);
  min-width: 260px;
  max-width: 360px;
  overflow: hidden;
}
.popover-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-muted-bg);
}
.popover-date { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }
.popover-close { background: none; border: none; cursor: pointer; color: var(--color-muted); padding: 2px; font-size: var(--font-size-sm); }

.popover-events { padding: var(--sp-3) var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-3); }
.popover-event { display: flex; align-items: flex-start; gap: var(--sp-3); }
.popover-event-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; margin-top: 4px; }
.popover-event-body { display: flex; flex-direction: column; gap: 2px; }
.popover-event-type { font-size: var(--font-size-xs); color: var(--color-muted); display: flex; align-items: center; gap: var(--sp-1); }
.popover-event-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); }

@media (max-width: 900px) { .months-grid { grid-template-columns: repeat(3, 1fr); } }
@media (max-width: 640px) { .months-grid { grid-template-columns: repeat(2, 1fr); } }
</style>
