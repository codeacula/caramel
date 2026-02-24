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
      <div class="main-area">
        <div class="main-placeholder">
          <div class="placeholder-icon" aria-hidden="true">🎲</div>
          <h1 class="placeholder-title">Adventure Awaits</h1>
          <p class="placeholder-body">
            No adventure is running right now.<br />
            Use <code>!adventure start</code> in chat to begin.
          </p>
        </div>
      </div>

      <!-- Bottom 1/3: Control Panel -->
      <div class="control-panel-container">
        <ControlPanel />
      </div>
    </main>

    <!-- Right sidebar (Chat) -->
    <aside class="right-sidebar">
      <TwitchChat />
    </aside>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  height: 100vh;
  overflow: hidden;
  gap: 0;
}

/* ── Left sidebar ─────────────────────────────────────────────────────────── */
.left-sidebar {
  width: 240px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  border-right: 1px solid #2a2a2e;
  background: #0e0e10;
  overflow: hidden;
}

/* ── Center column ────────────────────────────────────────────────────────── */
.center-column {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #0e0e10;
}

.main-area {
  flex: 2;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 24px;
}

.control-panel-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 24px;
  border-top: 1px solid #2a2a2e;
}

.main-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  text-align: center;
  color: #6b6b7d;
  user-select: none;
}

.placeholder-icon {
  font-size: 64px;
  line-height: 1;
  filter: grayscale(0.3) opacity(0.7);
}

.placeholder-title {
  font-size: 24px;
  font-weight: 700;
  color: #adadb8;
  letter-spacing: -0.01em;
}

.placeholder-body {
  font-size: 14px;
  line-height: 1.7;
  max-width: 340px;
}

.placeholder-body code {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 4px;
  background: #18181b;
  border: 1px solid #3a3a40;
  color: #bf94ff;
  font-size: 13px;
  font-family: "Fira Code", "Cascadia Code", "Consolas", monospace;
}

/* ── Right sidebar ────────────────────────────────────────────────────────── */
.right-sidebar {
  width: 360px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #2a2a2e;
  overflow: hidden;
}

/* ── Responsive: collapse sidebar on narrow screens ──────────────────────── */
@media (max-width: 1024px) {
  .app-layout {
    flex-direction: column;
  }

  .left-sidebar,
  .right-sidebar {
    width: 100%;
    flex: 0 0 auto;
    min-height: 200px;
    border: none;
    border-top: 1px solid #2a2a2e;
  }

  .left-sidebar {
    border-top: none;
    border-bottom: 1px solid #2a2a2e;
  }

  .center-column {
    flex: 1;
  }

  .main-area {
    flex: 0 0 auto;
    min-height: 200px;
    padding: 16px;
  }
}
</style>
