<script setup lang="ts">
import TwitchChat from "./components/TwitchChat.vue";
</script>

<template>
  <div class="app-layout">
    <!-- Main adventure area -->
    <main class="main-area">
      <div class="main-placeholder">
        <div class="placeholder-icon" aria-hidden="true">🎲</div>
        <h1 class="placeholder-title">Adventure Awaits</h1>
        <p class="placeholder-body">
          No adventure is running right now.<br />
          Use <code>!adventure start</code> in chat to begin.
        </p>
      </div>
    </main>

    <!-- Chat sidebar -->
    <aside class="chat-sidebar">
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

/* ── Main area ────────────────────────────────────────────────────────────── */
.main-area {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  padding: 24px;
  background: #0e0e10;
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

/* ── Chat sidebar ─────────────────────────────────────────────────────────── */
.chat-sidebar {
  width: 360px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #2a2a2e;
  overflow: hidden;
}

/* ── Responsive: collapse sidebar on narrow screens ──────────────────────── */
@media (max-width: 640px) {
  .app-layout {
    flex-direction: column;
  }

  .main-area {
    flex: 0 0 auto;
    min-height: 200px;
    padding: 16px;
  }

  .chat-sidebar {
    width: 100%;
    flex: 1;
    border-left: none;
    border-top: 1px solid #2a2a2e;
  }
}
</style>
