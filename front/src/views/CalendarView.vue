<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getCalendarEvents, type CalendarEvent } from '../api/calendar'

type CalView = 'month' | 'year' | 'week' | 'day' | 'schedule'

const today = new Date()

// ── Core state ────────────────────────────────────────────────────────────────
const currentDate = ref(new Date(today.getFullYear(), today.getMonth(), 1))
const currentView = ref<CalView>('month')
const showViewDropdown = ref(false)
const loading = ref(true)
const events = ref<CalendarEvent[]>([])
const visibleCourses = ref<Set<number>>(new Set())
const popover = ref<{ year: number; month: number; day: number; events: CalendarEvent[] } | null>(null)

// ── Constants ─────────────────────────────────────────────────────────────────
const MONTHS_ES = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre']
const MONTHS_EN = ['January','February','March','April','May','June','July','August','September','October','November','December']
const WD_SHORT  = ['Mon','Tue','Wed','Thu','Fri','Sat','Sun']
const PALETTE   = ['#6366f1','#0ea5e9','#10b981','#f59e0b','#ec4899','#14b8a6','#f97316','#8b5cf6','#84cc16','#ef4444']

const VIEW_LABELS:    Record<CalView, string> = { day:'Day', week:'Week', month:'Month', year:'Year', schedule:'Schedule' }
const VIEW_SHORTCUTS: Record<CalView, string> = { day:'D',   week:'W',   month:'M',     year:'Y',   schedule:'A' }
const VIEW_OPTIONS: CalView[] = ['day', 'week', 'month', 'year', 'schedule']

// ── Derived date parts ────────────────────────────────────────────────────────
const currentYear  = computed(() => currentDate.value.getFullYear())
const currentMonth = computed(() => currentDate.value.getMonth())

// ── Header date label ─────────────────────────────────────────────────────────
const dateLabel = computed(() => {
  const yr = currentYear.value
  const mo = currentMonth.value
  switch (currentView.value) {
    case 'day':
      return `${MONTHS_EN[mo]} ${currentDate.value.getDate()}, ${yr}`
    case 'week': {
      const s = weekStart(currentDate.value)
      const e = new Date(s); e.setDate(s.getDate() + 6)
      if (s.getMonth() === e.getMonth()) return `${MONTHS_EN[s.getMonth()]} ${s.getFullYear()}`
      const ey = e.getFullYear()
      return `${MONTHS_EN[s.getMonth()].slice(0,3)} – ${MONTHS_EN[e.getMonth()].slice(0,3)} ${ey}`
    }
    case 'month':
      return `${MONTHS_EN[mo]} ${yr}`
    case 'year':
      return `${yr}`
    case 'schedule': {
      const e = new Date(currentDate.value); e.setMonth(e.getMonth() + 5)
      const ey = e.getFullYear()
      return `${MONTHS_EN[mo].slice(0,3)} – ${MONTHS_EN[e.getMonth()].slice(0,3)} ${yr === ey ? yr : ey}`
    }
  }
})

// ── Navigation ────────────────────────────────────────────────────────────────
function goToday() {
  currentDate.value = new Date(today.getFullYear(), today.getMonth(), today.getDate())
}

function goPrev() {
  const d = new Date(currentDate.value)
  switch (currentView.value) {
    case 'day':      d.setDate(d.getDate() - 1);         break
    case 'week':     d.setDate(d.getDate() - 7);         break
    case 'month':    d.setMonth(d.getMonth() - 1);       break
    case 'year':     d.setFullYear(d.getFullYear() - 1); break
    case 'schedule': d.setMonth(d.getMonth() - 1);       break
  }
  currentDate.value = d
}

function goNext() {
  const d = new Date(currentDate.value)
  switch (currentView.value) {
    case 'day':      d.setDate(d.getDate() + 1);         break
    case 'week':     d.setDate(d.getDate() + 7);         break
    case 'month':    d.setMonth(d.getMonth() + 1);       break
    case 'year':     d.setFullYear(d.getFullYear() + 1); break
    case 'schedule': d.setMonth(d.getMonth() + 1);       break
  }
  currentDate.value = d
}

function setView(v: CalView) {
  currentView.value = v
  showViewDropdown.value = false
}

// ── Keyboard shortcuts ────────────────────────────────────────────────────────
function onKeyDown(e: KeyboardEvent) {
  const tag = (e.target as HTMLElement).tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return
  const map: Record<string, CalView> = { D:'day', W:'week', M:'month', Y:'year', A:'schedule' }
  const v = map[e.key.toUpperCase()]
  if (v) setView(v)
}

function onDocClick(e: MouseEvent) {
  if (!(e.target as HTMLElement).closest('.view-dropdown-wrap'))
    showViewDropdown.value = false
}

// ── Lifecycle ─────────────────────────────────────────────────────────────────
onMounted(async () => {
  window.addEventListener('keydown', onKeyDown)
  document.addEventListener('click', onDocClick)
  try {
    const res = await getCalendarEvents()
    events.value = res.data
    visibleCourses.value = new Set(res.data.map((e: CalendarEvent) => e.idCurso))
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeyDown)
  document.removeEventListener('click', onDocClick)
})

// ── Course helpers ────────────────────────────────────────────────────────────
const courseIds = computed(() => [...new Set(events.value.map(e => e.idCurso))])
function courseColor(id: number) { return PALETTE[id % PALETTE.length] }
function courseName(id: number) {
  return events.value.find(e => e.idCurso === id && e.tipo === 'inicio_curso')?.titulo ?? `Curso ${id}`
}
function toggleCourse(id: number) {
  const next = new Set(visibleCourses.value)
  if (next.has(id)) next.delete(id); else next.add(id)
  visibleCourses.value = next
}

// ── Event helpers ─────────────────────────────────────────────────────────────
const filteredEvents = computed(() =>
  events.value.filter(e => visibleCourses.value.has(e.idCurso))
)

const eventsByDate = computed(() => {
  const map = new Map<string, CalendarEvent[]>()
  for (const ev of filteredEvents.value) {
    const list = map.get(ev.fecha) ?? []; list.push(ev); map.set(ev.fecha, list)
  }
  return map
})

function eventsForDay(year: number, month: number, day: number): CalendarEvent[] {
  const key = `${year}-${String(month + 1).padStart(2,'0')}-${String(day).padStart(2,'0')}`
  return eventsByDate.value.get(key) ?? []
}

function eventColor(ev: CalendarEvent): string {
  if (ev.tipo === 'inicio_curso') return '#2563eb'
  if (ev.tipo === 'fin_curso')    return '#7c3aed'
  if (ev.tipoContenido === 'Examen' || ev.tipoContenido === 'Quiz') return '#dc2626'
  if (ev.tipoContenido === 'Documento' || ev.tipoContenido === 'Enlace') return '#16a34a'
  return '#f59e0b'
}

function eventTypeIcon(ev: CalendarEvent): string {
  if (ev.tipo === 'inicio_curso') return 'fas fa-play-circle'
  if (ev.tipo === 'fin_curso')    return 'fas fa-flag-checkered'
  if (ev.tipoContenido === 'Examen') return 'fas fa-file-signature'
  if (ev.tipoContenido === 'Quiz')   return 'fas fa-question-circle'
  return 'fas fa-laptop-code'
}

function eventTypeLabel(ev: CalendarEvent): string {
  if (ev.tipo === 'inicio_curso') return 'Inicio de curso'
  if (ev.tipo === 'fin_curso')    return 'Fin de curso'
  return ev.tipoContenido ?? 'Entrega'
}

// ── Popover ───────────────────────────────────────────────────────────────────
function selectDay(year: number, month: number, day: number | null) {
  if (!day) return
  const dayEvents = eventsForDay(year, month, day)
  if (popover.value?.year === year && popover.value?.month === month && popover.value?.day === day) {
    popover.value = null; return
  }
  popover.value = dayEvents.length ? { year, month, day, events: dayEvents } : null
}

// ── Month / Year view helpers ─────────────────────────────────────────────────
interface CalDay { day: number | null; isToday: boolean; isWeekend: boolean }

function buildMonth(year: number, month: number): CalDay[][] {
  const firstDow = new Date(year, month, 1).getDay()
  const offset   = (firstDow + 6) % 7
  const total    = new Date(year, month + 1, 0).getDate()
  const cells: (CalDay | null)[] = []
  for (let i = 0; i < offset; i++) cells.push(null)
  for (let d = 1; d <= total; d++) {
    const dow = (offset + d - 1) % 7
    cells.push({
      day: d,
      isToday: year === today.getFullYear() && month === today.getMonth() && d === today.getDate(),
      isWeekend: dow >= 5,
    })
  }
  while (cells.length % 7 !== 0) cells.push(null)
  const weeks: CalDay[][] = []
  for (let i = 0; i < cells.length; i += 7)
    weeks.push(cells.slice(i, i + 7).map(c => c ?? { day: null, isToday: false, isWeekend: false }))
  return weeks
}

const monthWeeks  = computed(() => buildMonth(currentYear.value, currentMonth.value))
const yearMonths  = computed(() =>
  MONTHS_ES.map((name, idx) => ({ name, index: idx, weeks: buildMonth(currentYear.value, idx) }))
)

// ── Week view ─────────────────────────────────────────────────────────────────
function weekStart(d: Date): Date {
  const r   = new Date(d)
  const dow = r.getDay()
  r.setDate(r.getDate() - (dow + 6) % 7)
  r.setHours(0, 0, 0, 0)
  return r
}

const weekDays = computed(() => {
  const start = weekStart(currentDate.value)
  return Array.from({ length: 7 }, (_, i) => {
    const d = new Date(start); d.setDate(start.getDate() + i)
    return {
      year: d.getFullYear(), month: d.getMonth(), day: d.getDate(),
      label: WD_SHORT[i],
      isToday:   d.toDateString() === today.toDateString(),
      isWeekend: i >= 5,
    }
  })
})

// ── Day view ──────────────────────────────────────────────────────────────────
const dayEvents = computed(() =>
  eventsForDay(currentDate.value.getFullYear(), currentDate.value.getMonth(), currentDate.value.getDate())
)

const dayLabel = computed(() => {
  const d = currentDate.value
  const wd = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'][d.getDay()]
  return `${wd}, ${MONTHS_EN[d.getMonth()]} ${d.getDate()}`
})

// ── Schedule view ─────────────────────────────────────────────────────────────
const scheduleGroups = computed(() => {
  const start = new Date(currentDate.value); start.setDate(1)
  const end   = new Date(start); end.setMonth(end.getMonth() + 6)
  const filtered = filteredEvents.value.filter(ev => {
    const d = new Date(ev.fecha + 'T00:00:00')
    return d >= start && d < end
  })
  const groups = new Map<string, CalendarEvent[]>()
  for (const ev of filtered) {
    const list = groups.get(ev.fecha) ?? []; list.push(ev); groups.set(ev.fecha, list)
  }
  return [...groups.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([fecha, evs]) => ({ fecha, events: evs }))
})

function formatScheduleDate(fecha: string): string {
  const d  = new Date(fecha + 'T00:00:00')
  const wd = ['Dom','Lun','Mar','Mié','Jue','Vie','Sáb'][d.getDay()]
  return `${wd}, ${d.getDate()} de ${MONTHS_ES[d.getMonth()]} ${d.getFullYear()}`
}

function isScheduleDateToday(fecha: string): boolean {
  return fecha === `${today.getFullYear()}-${String(today.getMonth()+1).padStart(2,'0')}-${String(today.getDate()).padStart(2,'0')}`
}
</script>

<template>
  <div class="page">
    <div class="cal-container">

      <!-- ═══════════════════ HEADER ═══════════════════ -->
      <div class="cal-header">
        <div class="cal-header-left">
          <button class="btn-today" @click="goToday">Today</button>
          <div class="nav-arrows">
            <button class="nav-btn nav-btn-prev" @click="goPrev" aria-label="Previous">
              <i class="fas fa-chevron-left"></i>
            </button>
            <button class="nav-btn nav-btn-next" @click="goNext" aria-label="Next">
              <i class="fas fa-chevron-right"></i>
            </button>
          </div>
          <span class="date-label">{{ dateLabel }}</span>
        </div>

        <div class="view-dropdown-wrap">
          <button class="view-selector-btn" @click.stop="showViewDropdown = !showViewDropdown">
            {{ VIEW_LABELS[currentView] }}
            <i class="fas fa-chevron-down view-chevron" :class="{ 'view-chevron--open': showViewDropdown }"></i>
          </button>
          <div v-if="showViewDropdown" class="view-menu">
            <button
              v-for="v in VIEW_OPTIONS"
              :key="v"
              class="view-menu-item"
              :class="{ 'view-menu-item--active': currentView === v }"
              @click="setView(v)"
            >
              <span>{{ VIEW_LABELS[v] }}</span>
              <kbd class="view-shortcut">{{ VIEW_SHORTCUTS[v] }}</kbd>
            </button>
          </div>
        </div>
      </div>

      <!-- Course filter -->
      <div v-if="courseIds.length > 1" class="course-filter">
        <button
          v-for="id in courseIds" :key="id"
          class="course-chip"
          :class="{ 'course-chip--off': !visibleCourses.has(id) }"
          :style="{ '--chip-color': courseColor(id) }"
          @click="toggleCourse(id)"
        >
          <span class="chip-dot" :style="{ background: courseColor(id) }"></span>
          {{ courseName(id) }}
        </button>
      </div>

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <template v-else>

        <!-- ═══════════════════ MONTH VIEW ═══════════════════ -->
        <div v-if="currentView === 'month'" class="month-view">
          <div class="mv-weekdays">
            <span v-for="wd in WD_SHORT" :key="wd" class="mv-wd">{{ wd }}</span>
          </div>
          <div class="mv-grid">
            <template v-for="(week, wi) in monthWeeks" :key="wi">
              <div
                v-for="(cell, ci) in week" :key="ci"
                class="mv-cell"
                :class="{
                  'mv-cell--empty':    !cell.day,
                  'mv-cell--weekend':  cell.isWeekend,
                  'mv-cell--today':    cell.isToday,
                }"
                @click="selectDay(currentYear, currentMonth, cell.day)"
              >
                <span v-if="cell.day" class="mv-day-num" :class="{ 'mv-day-num--today': cell.isToday }">
                  {{ cell.day }}
                </span>
                <div v-if="cell.day" class="mv-events">
                  <div
                    v-for="(ev, ei) in eventsForDay(currentYear, currentMonth, cell.day!).slice(0, 3)"
                    :key="ei"
                    class="mv-event-pill"
                    :style="{ '--pill-color': eventColor(ev) }"
                  >
                    <span class="mv-event-dot"></span>
                    <span class="mv-event-title">{{ ev.titulo }}</span>
                  </div>
                  <div
                    v-if="eventsForDay(currentYear, currentMonth, cell.day!).length > 3"
                    class="mv-event-more"
                  >+{{ eventsForDay(currentYear, currentMonth, cell.day!).length - 3 }} more</div>
                </div>
              </div>
            </template>
          </div>
        </div>

        <!-- ═══════════════════ YEAR VIEW ═══════════════════ -->
        <div v-else-if="currentView === 'year'" class="months-grid">
          <div class="month-card" v-for="m in yearMonths" :key="m.index">
            <div class="month-name">{{ m.name }}</div>
            <div class="weekdays-row">
              <span v-for="wd in ['Lu','Ma','Mi','Ju','Vi','Sa','Do']" :key="wd" class="wd-cell">{{ wd }}</span>
            </div>
            <div class="week-row" v-for="(week, wi) in m.weeks" :key="wi">
              <div v-for="(cell, ci) in week" :key="ci" class="day-cell-wrap">
                <button
                  class="day-cell"
                  :class="{
                    'day-empty':      !cell.day,
                    'day-today':      cell.isToday,
                    'day-weekend':    cell.isWeekend && !cell.isToday,
                    'day-has-events': cell.day && eventsForDay(currentYear, m.index, cell.day!).length > 0,
                  }"
                  :disabled="!cell.day"
                  @click="selectDay(currentYear, m.index, cell.day)"
                >{{ cell.day ?? '' }}</button>
                <div v-if="cell.day" class="event-dots">
                  <span
                    v-for="(ev, ei) in eventsForDay(currentYear, m.index, cell.day!).slice(0, 3)"
                    :key="ei"
                    class="event-dot"
                    :style="{ background: eventColor(ev) }"
                  ></span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- ═══════════════════ WEEK VIEW ═══════════════════ -->
        <div v-else-if="currentView === 'week'" class="week-view">
          <div class="wv-header">
            <div
              v-for="col in weekDays" :key="col.day"
              class="wv-col-head"
              :class="{ 'wv-col-head--today': col.isToday, 'wv-col-head--weekend': col.isWeekend }"
            >
              <span class="wv-wd-label">{{ col.label }}</span>
              <span class="wv-day-num" :class="{ 'wv-day-num--today': col.isToday }">{{ col.day }}</span>
            </div>
          </div>
          <div class="wv-body">
            <div
              v-for="col in weekDays" :key="col.day"
              class="wv-col"
              :class="{ 'wv-col--today': col.isToday, 'wv-col--weekend': col.isWeekend }"
            >
              <div
                v-if="eventsForDay(col.year, col.month, col.day).length === 0"
                class="wv-empty"
              ></div>
              <div
                v-for="(ev, ei) in eventsForDay(col.year, col.month, col.day)"
                :key="ei"
                class="wv-event"
                :style="{ '--ev-color': eventColor(ev) }"
                @click="selectDay(col.year, col.month, col.day)"
              >
                <i :class="eventTypeIcon(ev)" class="wv-event-icon"></i>
                <span class="wv-event-title">{{ ev.titulo }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- ═══════════════════ DAY VIEW ═══════════════════ -->
        <div v-else-if="currentView === 'day'" class="day-view">
          <div class="dv-header">
            <h2 class="dv-date">{{ dayLabel }}</h2>
          </div>
          <div v-if="dayEvents.length === 0" class="dv-empty">
            <i class="fas fa-calendar-check dv-empty-icon"></i>
            <p>No hay eventos para este día.</p>
          </div>
          <div v-else class="dv-events">
            <div
              v-for="(ev, i) in dayEvents" :key="i"
              class="dv-event"
              :style="{ '--ev-color': eventColor(ev) }"
            >
              <div class="dv-event-stripe"></div>
              <div class="dv-event-body">
                <div class="dv-event-type">
                  <i :class="eventTypeIcon(ev)"></i> {{ eventTypeLabel(ev) }}
                </div>
                <div class="dv-event-title">{{ ev.titulo }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- ═══════════════════ SCHEDULE VIEW ═══════════════════ -->
        <div v-else-if="currentView === 'schedule'" class="schedule-view">
          <div v-if="scheduleGroups.length === 0" class="sv-empty">
            <i class="fas fa-calendar-check sv-empty-icon"></i>
            <p>No hay eventos en los próximos 6 meses.</p>
          </div>
          <div v-else class="sv-list">
            <div v-for="group in scheduleGroups" :key="group.fecha" class="sv-group">
              <div class="sv-date-label" :class="{ 'sv-date-label--today': isScheduleDateToday(group.fecha) }">
                {{ formatScheduleDate(group.fecha) }}
                <span v-if="isScheduleDateToday(group.fecha)" class="sv-today-badge">Today</span>
              </div>
              <div class="sv-events">
                <div
                  v-for="(ev, i) in group.events" :key="i"
                  class="sv-event"
                  :style="{ '--ev-color': eventColor(ev) }"
                >
                  <div class="sv-event-dot"></div>
                  <div class="sv-event-body">
                    <div class="sv-event-type">
                      <i :class="eventTypeIcon(ev)"></i> {{ eventTypeLabel(ev) }}
                    </div>
                    <div class="sv-event-title">{{ ev.titulo }}</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

      </template>
    </div>

    <!-- ═══════════════════ POPOVER ═══════════════════ -->
    <Teleport to="body">
      <div v-if="popover" class="popover-overlay" @click.self="popover = null">
        <div class="popover-box">
          <div class="popover-header">
            <span class="popover-date">
              {{ popover.day }} de {{ MONTHS_ES[popover.month] }} {{ popover.year }}
            </span>
            <button class="popover-close" @click="popover = null"><i class="fas fa-times"></i></button>
          </div>
          <div class="popover-events">
            <div v-for="(ev, i) in popover.events" :key="i" class="popover-event">
              <div class="popover-event-dot" :style="{ background: eventColor(ev) }"></div>
              <div class="popover-event-body">
                <div class="popover-event-type" :style="{ color: eventColor(ev) }">
                  <i :class="eventTypeIcon(ev)"></i> {{ eventTypeLabel(ev) }}
                </div>
                <div class="popover-event-title">{{ ev.titulo }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
/* ── Container ─────────────────────────────────────────────────────────────── */
.cal-container {
  max-width: 1280px;
  width: 100%;
  margin: 0 auto;
  padding: var(--sp-6) var(--sp-6) var(--sp-10);
  box-sizing: border-box;
}

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

/* ── Header ────────────────────────────────────────────────────────────────── */
.cal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-4);
  margin-bottom: var(--sp-5);
  flex-wrap: wrap;
}

.cal-header-left {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  flex-wrap: wrap;
}

/* Today button */
.btn-today {
  height: 36px;
  padding: 0 var(--sp-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  background: var(--color-surface);
  color: var(--color-text);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
  white-space: nowrap;
}
.btn-today:hover { background: var(--color-muted-bg); border-color: var(--color-primary); color: var(--color-primary); }

/* Prev / Next arrows */
.nav-arrows {
  display: flex;
}
.nav-btn {
  width: 36px;
  height: 36px;
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-xs);
  transition: background 0.15s, border-color 0.15s, color 0.15s;
}
.nav-btn:hover { background: var(--color-muted-bg); border-color: var(--color-primary); color: var(--color-primary); }
.nav-btn-prev { border-radius: var(--radius-md) 0 0 var(--radius-md); border-right: none; }
.nav-btn-next { border-radius: 0 var(--radius-md) var(--radius-md) 0; }

/* Date label */
.date-label {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  white-space: nowrap;
}

/* View selector dropdown */
.view-dropdown-wrap { position: relative; }

.view-selector-btn {
  height: 36px;
  padding: 0 var(--sp-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  background: var(--color-surface);
  color: var(--color-text);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  white-space: nowrap;
  transition: background 0.15s, border-color 0.15s;
}
.view-selector-btn:hover { background: var(--color-muted-bg); border-color: var(--color-primary); color: var(--color-primary); }

.view-chevron { font-size: 0.65rem; transition: transform 0.2s; }
.view-chevron--open { transform: rotate(180deg); }

.view-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  z-index: 100;
  min-width: 160px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
  overflow: hidden;
  animation: fadeIn 0.1s ease;
}
@keyframes fadeIn { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }

.view-menu-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: var(--sp-2) var(--sp-4);
  border: none;
  background: none;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  color: var(--color-text);
  cursor: pointer;
  transition: background 0.1s;
  text-align: left;
}
.view-menu-item:hover { background: var(--color-muted-bg); }
.view-menu-item--active { background: #e8f0fe; color: var(--color-primary); font-weight: var(--font-weight-semibold); }
.view-menu-item--active:hover { background: #d2e3fc; }

.view-shortcut {
  font-family: var(--font-mono);
  font-size: 0.7rem;
  padding: 1px 5px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-muted-bg);
  color: var(--color-muted);
  line-height: 1.6;
}
.view-menu-item--active .view-shortcut { border-color: #93b4f9; background: #dce8fd; color: var(--color-primary); }

/* ── Course filter ─────────────────────────────────────────────────────────── */
.course-filter {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-2);
  margin-bottom: var(--sp-5);
}
.course-chip {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-1) var(--sp-3);
  border: 1.5px solid var(--chip-color, var(--color-border));
  border-radius: var(--radius-full);
  background: none;
  font-family: var(--font-sans);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  cursor: pointer;
  transition: opacity 0.15s;
}
.course-chip--off { opacity: 0.35; }
.chip-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }

/* ── MONTH VIEW ────────────────────────────────────────────────────────────── */
.month-view {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  background: var(--color-surface);
}

.mv-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: var(--color-muted-bg);
  border-bottom: 1px solid var(--color-border);
}
.mv-wd {
  padding: var(--sp-2) var(--sp-1);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-muted);
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.mv-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
}
.mv-cell {
  min-height: 110px;
  border-right: 1px solid var(--color-border);
  border-bottom: 1px solid var(--color-border);
  padding: var(--sp-2);
  cursor: pointer;
  transition: background 0.1s;
  box-sizing: border-box;
}
.mv-cell:nth-child(7n) { border-right: none; }
.mv-cell:hover:not(.mv-cell--empty) { background: #f5f7ff; }
.mv-cell--empty { background: var(--color-muted-bg); cursor: default; pointer-events: none; }
.mv-cell--weekend { background: #fafafa; }
.mv-cell--weekend:hover { background: #f0f2f8; }
.mv-cell--today { background: #eff6ff; }
.mv-cell--today:hover { background: #dbeafe; }

.mv-day-num {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  margin-bottom: var(--sp-1);
}
.mv-day-num--today {
  background: var(--color-primary);
  color: #fff;
  font-weight: var(--font-weight-bold);
}

.mv-events { display: flex; flex-direction: column; gap: 2px; }

.mv-event-pill {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 1px var(--sp-1);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--pill-color) 12%, transparent);
  font-size: 0.7rem;
  line-height: 1.5;
  overflow: hidden;
  cursor: pointer;
}
.mv-event-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--pill-color);
  flex-shrink: 0;
}
.mv-event-title {
  color: var(--color-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-weight: var(--font-weight-medium);
}
.mv-event-more {
  font-size: 0.68rem;
  color: var(--color-muted);
  padding-left: var(--sp-1);
}

/* ── YEAR VIEW ─────────────────────────────────────────────────────────────── */
.months-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: var(--sp-5);
}

.month-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-4);
  box-shadow: var(--shadow-sm);
}

.month-name {
  font-size: 0.95rem;
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  text-align: center;
  margin-bottom: var(--sp-3);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.weekdays-row { display: grid; grid-template-columns: repeat(7, 1fr); margin-bottom: var(--sp-1); }
.wd-cell { font-size: 0.75rem; font-weight: var(--font-weight-semibold); color: var(--color-muted); text-align: center; padding: 2px 0; }

.week-row { display: grid; grid-template-columns: repeat(7, 1fr); }
.day-cell-wrap { display: flex; flex-direction: column; align-items: center; }

.day-cell {
  font-size: 0.8rem;
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
.day-today { background: var(--color-primary); color: #fff; font-weight: var(--font-weight-bold); }
.day-has-events:not(.day-today) { font-weight: var(--font-weight-semibold); }

.event-dots { display: flex; gap: 2px; justify-content: center; min-height: 5px; margin-top: 1px; }
.event-dot { width: 4px; height: 4px; border-radius: 50%; flex-shrink: 0; }

/* ── WEEK VIEW ─────────────────────────────────────────────────────────────── */
.week-view {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  background: var(--color-surface);
}

.wv-header {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  background: var(--color-muted-bg);
  border-bottom: 1px solid var(--color-border);
}
.wv-col-head {
  padding: var(--sp-3) var(--sp-2);
  text-align: center;
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--sp-1);
}
.wv-col-head:last-child { border-right: none; }
.wv-col-head--weekend { background: #fafafa; }
.wv-col-head--today { background: #eff6ff; }

.wv-wd-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.wv-day-num {
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}
.wv-day-num--today {
  background: var(--color-primary);
  color: #fff;
  font-weight: var(--font-weight-bold);
}

.wv-body {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  min-height: 200px;
}
.wv-col {
  border-right: 1px solid var(--color-border);
  padding: var(--sp-2);
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}
.wv-col:last-child { border-right: none; }
.wv-col--weekend { background: #fafafa; }
.wv-col--today { background: #eff6ff; }

.wv-empty { flex: 1; }
.wv-event {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-1);
  padding: var(--sp-1) var(--sp-2);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--ev-color) 12%, transparent);
  border-left: 3px solid var(--ev-color);
  cursor: pointer;
  transition: background 0.1s;
}
.wv-event:hover { background: color-mix(in srgb, var(--ev-color) 20%, transparent); }
.wv-event-icon { color: var(--ev-color); font-size: 0.65rem; margin-top: 3px; flex-shrink: 0; }
.wv-event-title { font-size: 0.7rem; color: var(--color-text); line-height: 1.4; font-weight: var(--font-weight-medium); word-break: break-word; }

/* ── DAY VIEW ──────────────────────────────────────────────────────────────── */
.day-view {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
}
.dv-header {
  padding: var(--sp-4) var(--sp-5);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-muted-bg);
}
.dv-date {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.dv-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--sp-3);
  padding: var(--sp-12) var(--sp-6);
  color: var(--color-muted);
}
.dv-empty-icon { font-size: 2rem; }
.dv-empty p { margin: 0; font-size: var(--font-size-sm); }

.dv-events { padding: var(--sp-4) var(--sp-5); display: flex; flex-direction: column; gap: var(--sp-3); }
.dv-event {
  display: flex;
  align-items: stretch;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  overflow: hidden;
}
.dv-event-stripe { width: 4px; background: var(--ev-color); flex-shrink: 0; }
.dv-event-body { padding: var(--sp-3) var(--sp-4); flex: 1; }
.dv-event-type {
  font-size: var(--font-size-xs);
  color: var(--ev-color);
  font-weight: var(--font-weight-medium);
  display: flex;
  align-items: center;
  gap: var(--sp-1);
  margin-bottom: var(--sp-1);
}
.dv-event-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }

/* ── SCHEDULE VIEW ─────────────────────────────────────────────────────────── */
.schedule-view {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
}

.sv-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--sp-3);
  padding: var(--sp-12) var(--sp-6);
  color: var(--color-muted);
}
.sv-empty-icon { font-size: 2rem; }
.sv-empty p { margin: 0; font-size: var(--font-size-sm); }

.sv-list { display: flex; flex-direction: column; }

.sv-group { border-bottom: 1px solid var(--color-border); }
.sv-group:last-child { border-bottom: none; }

.sv-date-label {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-2) var(--sp-5);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-muted);
  background: var(--color-muted-bg);
  text-transform: uppercase;
  letter-spacing: 0.04em;
  border-bottom: 1px solid var(--color-border);
}
.sv-date-label--today { color: var(--color-primary); background: #eff6ff; }
.sv-today-badge {
  padding: 1px var(--sp-2);
  border-radius: var(--radius-full);
  background: var(--color-primary);
  color: #fff;
  font-size: 0.65rem;
  text-transform: none;
  letter-spacing: 0;
}

.sv-events { padding: var(--sp-2) var(--sp-5); display: flex; flex-direction: column; gap: var(--sp-2); }
.sv-event { display: flex; align-items: flex-start; gap: var(--sp-3); padding: var(--sp-1) 0; }
.sv-event-dot {
  width: 10px; height: 10px;
  border-radius: 50%;
  background: var(--ev-color);
  flex-shrink: 0;
  margin-top: 4px;
}
.sv-event-body { display: flex; flex-direction: column; gap: 2px; }
.sv-event-type {
  font-size: var(--font-size-xs);
  color: var(--ev-color);
  display: flex;
  align-items: center;
  gap: var(--sp-1);
  font-weight: var(--font-weight-medium);
}
.sv-event-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }

/* ── POPOVER ───────────────────────────────────────────────────────────────── */
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
  box-shadow: var(--shadow-lg);
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
.popover-date  { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }
.popover-close { background: none; border: none; cursor: pointer; color: var(--color-muted); padding: 2px; font-size: var(--font-size-sm); }

.popover-events { padding: var(--sp-3) var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-3); }
.popover-event  { display: flex; align-items: flex-start; gap: var(--sp-3); }
.popover-event-dot   { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; margin-top: 4px; }
.popover-event-body  { display: flex; flex-direction: column; gap: 2px; }
.popover-event-type  { font-size: var(--font-size-xs); color: var(--color-muted); display: flex; align-items: center; gap: var(--sp-1); }
.popover-event-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); }

/* ── RESPONSIVE ────────────────────────────────────────────────────────────── */
@media (max-width: 960px) {
  .months-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}

@media (max-width: 768px) {
  .cal-container { padding: var(--sp-4) var(--sp-3) var(--sp-8); }
  .date-label { font-size: var(--font-size-md); }

  /* Month view: shorter cells on tablet */
  .mv-cell { min-height: 80px; padding: var(--sp-1); }
  .mv-event-pill { display: none; }

  /* Week view: scrollable */
  .week-view { overflow-x: auto; }
  .wv-header,
  .wv-body   { min-width: 560px; }

  .months-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 480px) {
  .cal-header { gap: var(--sp-2); }
  .cal-header-left { gap: var(--sp-2); }
  .btn-today { height: 44px; }
  .nav-btn   { width: 44px; height: 44px; }
  .view-selector-btn { height: 44px; }
  .date-label { font-size: var(--font-size-sm); }

  /* Month view: compact cells, dots only */
  .mv-cell { min-height: 54px; padding: 3px; }
  .mv-event-more { display: none; }

  /* Show dots in month view on small mobile */
  .mv-events {
    flex-direction: row;
    flex-wrap: wrap;
    gap: 2px;
  }

  .months-grid { grid-template-columns: 1fr; }

  .sv-date-label { padding: var(--sp-2) var(--sp-3); }
  .sv-events     { padding: var(--sp-2) var(--sp-3); }
  .dv-header     { padding: var(--sp-3); }
  .dv-events     { padding: var(--sp-3); }
}
</style>
