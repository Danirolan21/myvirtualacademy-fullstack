<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

const dropdownOpen = ref(false)
const mobileOpen = ref(false)
const userMenuRef = ref<HTMLElement | null>(null)

function personalAreaRoute() {
  if (auth.isAdmin) return '/admin'
  if (['Profesor', 'Tutor', 'Administrador'].includes(auth.role)) return '/profesor'
  return '/estudiante'
}

async function handleLogout() {
  dropdownOpen.value = false
  mobileOpen.value = false
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

onMounted(() => document.addEventListener('mousedown', closeDropdown))
onUnmounted(() => document.removeEventListener('mousedown', closeDropdown))
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
</style>
