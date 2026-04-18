<script setup lang="ts">
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

async function handleLogout() {
  await auth.logout()
  router.push('/login')
}

function personalAreaRoute() {
  if (auth.isAdmin) return '/admin'
  if (['Profesor', 'Tutor', 'Administrador'].includes(auth.role)) return '/profesor'
  return '/estudiante'
}
</script>

<template>
  <header>
    <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light box-shadow py-4" style="background-color: black">
      <div class="container">
        <RouterLink class="navbar-brand" to="/">
          <img :src="'/logo.png'" width="50" height="50" alt="MVA" />
        </RouterLink>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse"
          aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
          <ul class="navbar-nav flex-grow-1">
            <li class="nav-item">
              <RouterLink class="nav-link fs-custom text-white" to="/">INICIO</RouterLink>
            </li>
            <li class="nav-item" v-if="auth.isAuthenticated">
              <RouterLink class="nav-link fs-custom text-white" :to="personalAreaRoute()">ÁREA PERSONAL</RouterLink>
            </li>
          </ul>

          <ul class="navbar-nav" v-if="!auth.isAuthenticated">
            <li class="nav-item">
              <RouterLink class="nav-link fs-custom text-white" to="/login">LOG IN</RouterLink>
            </li>
          </ul>

          <ul class="expand-menu mr-auto" v-else>
            <li>
              <a class="nav-link text-white fs-custom dropdown-toggle" href="#" id="navbarDropdown"
                role="button" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                <img :src="`/assets/images/users/${auth.user?.fotoPerfil}`" width="50" height="50" class="rounded-5" />
                {{ auth.user?.nombre }}
              </a>
              <ul class="dropdown-menu dropdown-menu-dark dropdown-menu-end">
                <li>
                  <RouterLink class="dropdown-item text-white" to="/perfil">PERFIL</RouterLink>
                </li>
                <li v-if="auth.isAdmin">
                  <RouterLink class="dropdown-item text-white" to="/register">REGISTRAR USUARIO</RouterLink>
                </li>
                <li>
                  <a class="dropdown-item text-white" href="#" @click.prevent="handleLogout">LOG OUT</a>
                </li>
              </ul>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  </header>
</template>
