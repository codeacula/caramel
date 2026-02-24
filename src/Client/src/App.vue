<script setup lang="ts">
import TwitchChat from "./components/TwitchChat.vue";
import ControlPanel from "./components/ControlPanel.vue";
</script>

<template>
  <div class="app-layout">
    <!-- Left sidebar -->
    <aside class="left-sidebar">
      <!-- Placeholder for left sidebar content -->
    </aside>

    <!-- Center column -->
    <main class="center-column">
      <!-- Top 2/3: Main adventure area -->
      <section class="main-area">
        <div class="main-placeholder">
          <div class="placeholder-icon" aria-hidden="true">🎲</div>
          <h1 class="placeholder-title">Adventure Awaits</h1>
          <p class="placeholder-body">
            No adventure is running right now.<br />
            Use <code>!adventure start</code> in chat to begin.
          </p>
        </div>
      </section>

      <!-- Bottom 1/3: Control Panel -->
      <section class="control-panel-container">
        <ControlPanel />
      </section>
    </main>

    <!-- Right sidebar (Chat) -->
    <aside class="right-sidebar">
      <TwitchChat />
    </aside>
  </div>
</template>

<style scoped>
.app-layout {
  display: grid;
  grid-template-columns: 240px 1fr 360px;
  block-size: 100vh;
  overflow: hidden;
  background: var(--bg-color);
  color: var(--text-primary);
}

/* ── Left sidebar ─────────────────────────────────────────────────────────── */
.left-sidebar {
  display: flex;
  flex-direction: column;
  border-inline-end: 1px solid var(--border-color);
  background: var(--surface-color);
  overflow: hidden;
}

/* ── Center column ────────────────────────────────────────────────────────── */
.center-column {
  display: flex;
  flex-direction: column;
  min-inline-size: 0;
  overflow: hidden;
  background: var(--bg-color);
}

.main-area {
  flex: 2;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: var(--space-xl);
}

.control-panel-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: var(--space-xl);
  border-block-start: 1px solid var(--border-color);
}

.main-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-md);
  text-align: center;
  color: var(--text-muted);
  user-select: none;
}

.placeholder-icon {
  font-size: var(--text-3xl);
  line-height: 1;
  filter: grayscale(0.3) opacity(0.7);
  transition: transform var(--transition-normal);
}
.placeholder-icon:hover {
  transform: scale(1.1);
}

.placeholder-title {
  font-size: var(--text-2xl);
  font-weight: 700;
  color: var(--text-secondary);
  letter-spacing: -0.01em;
}

.placeholder-body {
  font-size: var(--text-sm);
  line-height: 1.7;
  max-inline-size: 340px;
}

.placeholder-body code {
  display: inline-block;
  padding: 0.125em 0.375em;
  border-radius: var(--radius-sm);
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  color: var(--accent-primary);
  font-size: 0.85em;
  font-family: var(--font-mono);
}

/* ── Right sidebar ────────────────────────────────────────────────────────── */
.right-sidebar {
  display: flex;
  flex-direction: column;
  border-inline-start: 1px solid var(--border-color);
  overflow: hidden;
  background: var(--surface-color);
}

/* ── Responsive: collapse sidebar on narrow screens ──────────────────────── */
@media (max-width: 1024px) {
  .app-layout {
    grid-template-columns: 1fr;
    grid-template-rows: auto 1fr auto;
    overflow-y: auto;
  }

  .left-sidebar,
  .right-sidebar {
    min-block-size: 200px;
    border: none;
    border-block-start: 1px solid var(--border-color);
  }

  .left-sidebar {
    border-block-start: none;
    border-block-end: 1px solid var(--border-color);
  }

  .main-area {
    min-block-size: 200px;
    padding: var(--space-md);
  }
}
</style>
