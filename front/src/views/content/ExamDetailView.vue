<template>
  <div class="page">
    <div class="content-container">
      <div class="exam-stub">
        <div class="stub-icon"><i class="fas fa-circle-question"></i></div>
        <h2 class="stub-title">Funcionalidad de exámenes</h2>
        <p class="stub-desc">Los exámenes en línea están en desarrollo y estarán disponibles próximamente.</p>
        <button class="btn btn-outline-secondary" @click="$router.back()">
          <i class="fas fa-arrow-left"></i> Volver
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.content-container { max-width: 800px; margin: 0 auto; padding: var(--sp-6) var(--sp-4); }
.exam-stub {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-12) var(--sp-6);
  text-align: center;
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--sp-4);
}
.stub-icon { font-size: 3rem; color: var(--color-muted); }
.stub-title { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text); margin: 0; }
.stub-desc { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; max-width: 400px; }
</style>
