<template>
  <div class="page">
    <div class="notfound-container">
      <div class="notfound-code">
        <span>4</span><span class="code-zero">0</span><span>4</span>
      </div>
      <h1 class="notfound-title">¡Ups! Página no encontrada</h1>
      <p class="notfound-desc">La página que buscas no existe o ha sido movida. Comprueba la URL o vuelve al inicio.</p>
      <div class="notfound-actions">
        <RouterLink to="/" class="btn btn-primary">
          <i class="fas fa-home"></i> Volver al inicio
        </RouterLink>
        <button class="btn btn-outline-secondary" @click="$router.back()">
          <i class="fas fa-arrow-left"></i> Página anterior
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.notfound-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--sp-16) var(--sp-4);
  min-height: calc(100vh - var(--navbar-height) - 80px);
}

.notfound-code {
  font-size: 8rem;
  font-weight: var(--font-weight-bold);
  line-height: 1;
  color: var(--color-text);
  margin-bottom: var(--sp-6);
  letter-spacing: -0.05em;
}
.code-zero { color: var(--color-danger); }

.notfound-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin: 0 0 var(--sp-4);
}
.notfound-desc {
  font-size: var(--font-size-md);
  color: var(--color-muted);
  max-width: 480px;
  margin: 0 0 var(--sp-8);
  line-height: 1.6;
}
.notfound-actions {
  display: flex;
  gap: var(--sp-4);
  flex-wrap: wrap;
  justify-content: center;
}
</style>
