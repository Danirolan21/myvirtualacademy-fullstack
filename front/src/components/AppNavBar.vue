<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import { getNotifications, getUnreadCount, markRead, markAllRead } from '../api/notifications'
import type { Notificacion } from '../api/notifications'

const auth = useAuthStore()
const router = useRouter()

const dropdownOpen = ref(false)
const mobileOpen = ref(false)
const userMenuRef = ref<HTMLElement | null>(null)

// ─── Notifications ────────────────────────────────────────────────────────────
const bellOpen = ref(false)
const bellRef = ref<HTMLElement | null>(null)
const unreadCount = ref(0)
const notifications = ref<Notificacion[]>([])
let pollInterval: ReturnType<typeof setInterval> | null = null

async function fetchUnreadCount() {
  if (!auth.isAuthenticated) return
  try {
    const res = await getUnreadCount()
    unreadCount.value = res.data.count
  } catch { /* ignore */ }
}

async function openBell() {
  bellOpen.value = !bellOpen.value
  if (bellOpen.value) {
    try {
      const res = await getNotifications()
      notifications.value = res.data
      unreadCount.value = 0
    } catch { /* ignore */ }
  }
}

async function handleMarkAllRead() {
  await markAllRead()
  notifications.value = notifications.value.map(n => ({ ...n, leida: true }))
  unreadCount.value = 0
}

async function handleMarkRead(n: Notificacion) {
  if (!n.leida) {
    await markRead(n.idNotificacion)
    n.leida = true
  }
}

function notifIcon(tipo: string) {
  if (tipo === 'entrega') return 'fas fa-file-upload'
  if (tipo === 'calificacion') return 'fas fa-star'
  return 'fas fa-user-check'
}

function closeBell(e: MouseEvent) {
  if (bellRef.value && !bellRef.value.contains(e.target as Node)) {
    bellOpen.value = false
  }
}
// ─────────────────────────────────────────────────────────────────────────────

function personalAreaRoute() {
  if (auth.isAdmin) return '/admin'
  if (['Profesor', 'Tutor', 'Administrador'].includes(auth.role)) return '/profesor'
  return '/estudiante'
}

async function handleLogout() {
  dropdownOpen.value = false
  mobileOpen.value = false
  bellOpen.value = false
  if (pollInterval) clearInterval(pollInterval)
  await auth.logout()
  router.push('/login')
}

function closeDropdown(e: MouseEvent) {
  if (userMenuRef.value && !userMenuRef.value.contains(e.target as Node)) {
    dropdownOpen.value = false
  }
}

function closeMobileOnNav() {
  mobileOpen.value = false
}

function onMouseDown(e: MouseEvent) {
  closeDropdown(e)
  closeBell(e)
}

onMounted(() => {
  document.addEventListener('mousedown', onMouseDown)
  if (auth.isAuthenticated) {
    fetchUnreadCount()
    pollInterval = setInterval(fetchUnreadCount, 60_000)
  }
})
onUnmounted(() => {
  document.removeEventListener('mousedown', onMouseDown)
  if (pollInterval) clearInterval(pollInterval)
})
</script>

<template>
  <header class="navbar">
    <div class="navbar-inner">

      <!-- Logo -->
      <RouterLink to="/" class="navbar-brand" @click="closeMobileOnNav">
        <img src="/logo.png" width="44" height="44" alt="MyVirtualAcademy" />
      </RouterLink>

      <!-- Nav links -->
      <nav class="navbar-links" :class="{ open: mobileOpen }">
        <RouterLink class="nav-link" to="/" @click="closeMobileOnNav">INICIO</RouterLink>
        <RouterLink
          v-if="auth.isAuthenticated"
          class="nav-link"
          :to="personalAreaRoute()"
          @click="closeMobileOnNav"
        >ÁREA PERSONAL</RouterLink>
        <RouterLink class="nav-link" to="/calendario" @click="closeMobileOnNav">CALENDARIO</RouterLink>
        <a class="nav-link nav-link-placeholder" href="#" @click.prevent>MENSAJES</a>
      </nav>

      <!-- Right section -->
      <div class="navbar-right">
        <!-- Bell icon -->
        <div v-if="auth.isAuthenticated" class="bell-wrap" ref="bellRef">
          <button class="bell-btn" type="button" @click="openBell" :aria-label="`${unreadCount} notificaciones`">
            <i class="fas fa-bell"></i>
            <span v-if="unreadCount > 0" class="bell-badge">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
          </button>
          <div v-if="bellOpen" class="bell-dropdown">
            <div class="bell-header">
              <span class="bell-title">Notificaciones</span>
              <button v-if="notifications.some(n => !n.leida)" class="bell-mark-all" @click="handleMarkAllRead">
                Marcar todas leídas
              </button>
            </div>
            <div class="bell-list">
              <div v-if="!notifications.length" class="bell-empty">
                <i class="fas fa-bell-slash"></i>
                <span>Sin notificaciones</span>
              </div>
              <div
                v-for="n in notifications"
                :key="n.idNotificacion"
                class="bell-item"
                :class="{ 'bell-item--unread': !n.leida }"
                @click="handleMarkRead(n)"
              >
                <div class="bell-item-icon" :class="`bell-icon--${n.tipo}`">
                  <i :class="notifIcon(n.tipo)"></i>
                </div>
                <div class="bell-item-body">
                  <div class="bell-item-title">{{ n.titulo }}</div>
                  <div class="bell-item-msg">{{ n.mensaje }}</div>
                  <div class="bell-item-date">{{ new Date(n.fechaCreacion).toLocaleDateString('es-ES') }}</div>
                </div>
                <span v-if="!n.leida" class="bell-dot"></span>
              </div>
            </div>
          </div>
        </div>

        <!-- Authenticated: user menu -->
        <div v-if="auth.isAuthenticated" class="user-menu" ref="userMenuRef">
          <button class="user-toggle" @click="dropdownOpen = !dropdownOpen" type="button">
            <img
              :src="`/assets/images/users/${auth.user?.fotoPerfil}`"
              class="user-avatar"
              alt="Avatar"
            />
            <span class="user-name">{{ auth.user?.nombre }}</span>
            <i class="fas fa-chevron-down caret" :class="{ rotated: dropdownOpen }"></i>
          </button>
          <div v-if="dropdownOpen" class="user-dropdown">
            <RouterLink
              class="dropdown-item"
              to="/perfil"
              @click="dropdownOpen = false; closeMobileOnNav()"
            >PERFIL</RouterLink>
            <RouterLink
              v-if="auth.isAdmin"
              class="dropdown-item"
              to="/register"
              @click="dropdownOpen = false; closeMobileOnNav()"
            >REGISTRAR USUARIO</RouterLink>
            <a class="dropdown-item dropdown-item-danger" href="#" @click.prevent="handleLogout">LOG OUT</a>
          </div>
        </div>

        <!-- Not authenticated: login link -->
        <RouterLink v-else class="nav-link" to="/login">LOG IN</RouterLink>

        <!-- Hamburger for mobile -->
        <button class="mobile-toggle" type="button" @click="mobileOpen = !mobileOpen">
          <i :class="mobileOpen ? 'fas fa-times' : 'fas fa-bars'"></i>
        </button>
      </div>

    </div>
  </header>
</template>

<style scoped>
.navbar {
  background: var(--navbar-bg);
  height: var(--navbar-height);
  position: sticky;
  top: 0;
  z-index: 1000;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.35);
}

.navbar-inner {
  max-width: var(--container-max);
  margin-inline: auto;
  padding-inline: var(--container-padding);
  height: 100%;
  display: flex;
  align-items: center;
  gap: var(--sp-6);
}

.navbar-brand {
  display: flex;
  align-items: center;
  flex-shrink: 0;
  text-decoration: none;
}
.navbar-brand:hover { opacity: 0.85; }
.navbar-brand img { border-radius: var(--radius-sm); }

/* ── Nav links ── */
.navbar-links {
  display: flex;
  align-items: center;
  gap: var(--sp-1);
  flex: 1;
}

.nav-link {
  display: inline-block;
  color: rgba(255, 255, 255, 0.85);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  letter-spacing: 0.06em;
  padding: var(--sp-2) var(--sp-3);
  border-radius: var(--radius-md);
  transition: color 0.15s, background 0.15s;
  white-space: nowrap;
}
.nav-link:hover {
  color: var(--navbar-text);
  background: rgba(255, 255, 255, 0.1);
  text-decoration: none;
}
.nav-link.router-link-active {
  color: var(--navbar-text);
}
.nav-link-placeholder {
  opacity: 0.4;
  pointer-events: none;
  cursor: default;
}

/* ── Right section ── */
.navbar-right {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  margin-inline-start: auto;
}

/* ── User menu ── */
.user-menu {
  position: relative;
}

.user-toggle {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  background: none;
  border: none;
  color: var(--navbar-text);
  cursor: pointer;
  padding: var(--sp-2) var(--sp-3);
  border-radius: var(--radius-md);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  letter-spacing: 0.04em;
  transition: background 0.15s;
}
.user-toggle:hover {
  background: rgba(255, 255, 255, 0.1);
}

.user-avatar {
  width: 36px;
  height: 36px;
  border-radius: var(--radius-full);
  object-fit: cover;
  border: 2px solid rgba(255, 255, 255, 0.25);
  flex-shrink: 0;
}

.user-name {
  max-width: 130px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.caret {
  font-size: 0.7rem;
  transition: transform 0.2s;
}
.caret.rotated { transform: rotate(180deg); }

.user-dropdown {
  position: absolute;
  top: calc(100% + var(--sp-2));
  right: 0;
  min-width: 170px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  overflow: hidden;
  z-index: 1100;
}

.dropdown-item {
  display: block;
  padding: var(--sp-3) var(--sp-5);
  color: var(--color-text);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  letter-spacing: 0.05em;
  transition: background 0.15s;
}
.dropdown-item:hover {
  background: var(--color-muted-bg);
  text-decoration: none;
  color: #000;
}
.dropdown-item-danger:hover {
  background: #f8d7da;
  color: var(--color-danger);
}

/* ── Mobile hamburger ── */
.mobile-toggle {
  display: none;
  align-items: center;
  justify-content: center;
  background: none;
  border: none;
  color: var(--navbar-text);
  font-size: 1.2rem;
  cursor: pointer;
  padding: var(--sp-2);
  border-radius: var(--radius-md);
  transition: background 0.15s;
}
.mobile-toggle:hover { background: rgba(255, 255, 255, 0.1); }

/* ── Mobile breakpoint ── */
@media (max-width: 768px) {
  .mobile-toggle { display: flex; }

  .navbar-inner { position: relative; }

  .navbar-links {
    display: none;
    position: absolute;
    top: var(--navbar-height);
    left: 0;
    right: 0;
    background: var(--navbar-bg);
    flex-direction: column;
    align-items: flex-start;
    padding: var(--sp-3) var(--sp-6) var(--sp-4);
    gap: 0;
    border-top: 1px solid rgba(255, 255, 255, 0.12);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.4);
  }
  .navbar-links.open { display: flex; }
  .navbar-links .nav-link {
    width: 100%;
    padding: var(--sp-3) 0;
    border-radius: 0;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  }
  .navbar-links .nav-link:last-child { border-bottom: none; }

  .user-name { display: none; }
}

/* ── Bell ── */
.bell-wrap { position: relative; }

.bell-btn {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  background: none;
  border: none;
  color: var(--navbar-text);
  font-size: 1.1rem;
  cursor: pointer;
  padding: var(--sp-2) var(--sp-2);
  border-radius: var(--radius-md);
  transition: background 0.15s;
}
.bell-btn:hover { background: rgba(255, 255, 255, 0.1); }

.bell-badge {
  position: absolute;
  top: 2px;
  right: 2px;
  min-width: 16px;
  height: 16px;
  background: #ef4444;
  color: #fff;
  border-radius: 999px;
  font-size: 10px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 3px;
  line-height: 1;
}

.bell-dropdown {
  position: absolute;
  top: calc(100% + var(--sp-2));
  right: 0;
  width: 340px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  z-index: 1100;
  overflow: hidden;
}

.bell-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
}
.bell-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }
.bell-mark-all {
  background: none; border: none;
  font-size: var(--font-size-xs); color: var(--color-primary);
  cursor: pointer; padding: 0; font-family: var(--font-sans);
}
.bell-mark-all:hover { text-decoration: underline; }

.bell-list { max-height: 360px; overflow-y: auto; }

.bell-empty {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-2); padding: var(--sp-8) var(--sp-4);
  color: var(--color-muted); font-size: var(--font-size-sm);
}
.bell-empty i { font-size: 2rem; opacity: 0.4; }

.bell-item {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-3);
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
  cursor: pointer;
  transition: background 0.12s;
  position: relative;
}
.bell-item:last-child { border-bottom: none; }
.bell-item:hover { background: var(--color-muted-bg); }
.bell-item--unread { background: #eff6ff; }
.bell-item--unread:hover { background: #dbeafe; }

.bell-item-icon {
  width: 34px; height: 34px; flex-shrink: 0;
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-sm);
}
.bell-icon--entrega { background: #dcfce7; color: #16a34a; }
.bell-icon--calificacion { background: #fef9c3; color: #ca8a04; }
.bell-icon--inscripcion { background: #ede9fe; color: #7c3aed; }

.bell-item-body { flex: 1; min-width: 0; }
.bell-item-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }
.bell-item-msg { font-size: var(--font-size-xs); color: var(--color-muted); margin-top: 2px; line-height: 1.4; }
.bell-item-date { font-size: var(--font-size-xs); color: var(--color-muted); margin-top: var(--sp-1); }

.bell-dot {
  width: 8px; height: 8px;
  background: #3b82f6;
  border-radius: 50%;
  flex-shrink: 0;
  margin-top: 6px;
}

@media (max-width: 400px) {
  .bell-dropdown { width: calc(100vw - 2rem); right: -3rem; }
}
</style>
