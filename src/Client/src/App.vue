<script setup lang="ts">
import { ref } from "vue";
import { Toaster } from "vue-sonner";
import ControlPanel from "./components/ControlPanel.vue";
import TwitchChat from "./components/TwitchChat.vue";
import TwitchOAuthSetup from "./components/TwitchOAuthSetup.vue";

type ScreenId = "chat" | "settings";

type ScreenNavItem = {
  id: ScreenId;
  label: string;
  icon: string;
  description: string;
};

const screenNavItems: ScreenNavItem[] = [
  {
    id: "chat",
    label: "Chat",
    icon: "💬",
    description: "Live chat operations view",
  },
  {
    id: "settings",
    label: "Settings",
    icon: "⚙️",
    description: "Configuration and future screens",
  },
];

const activeScreen = ref<ScreenId>("chat");
</script>

<template>
  <div class="app-shell">
    <header class="top-nav" role="navigation" aria-label="Primary">
      <div class="brand">
        <span class="brand-mark" aria-hidden="true">🍯</span>
        <span class="brand-title">Caramel Control Center</span>
      </div>

      <nav class="screen-nav" aria-label="Screens">
        <button
          v-for="screen in screenNavItems"
          :key="screen.id"
          class="nav-btn"
          :class="{ active: activeScreen === screen.id }"
          :aria-pressed="activeScreen === screen.id"
          :title="screen.description"
          @click="activeScreen = screen.id"
        >
          <span class="nav-icon" aria-hidden="true">{{ screen.icon }}</span>
          <span>{{ screen.label }}</span>
        </button>
      </nav>
    </header>

    <div class="screen-content">
      <div v-if="activeScreen === 'chat'" class="app-layout">
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
          <ControlPanel class="control-panel-container" />
        </main>

        <!-- Right sidebar (Chat) -->
        <aside class="right-sidebar">
          <TwitchChat />
        </aside>
      </div>

      <main v-else class="settings-screen">
        <section class="settings-card full-width">
          <TwitchOAuthSetup />
        </section>
      </main>
    </div>
    <Toaster position="top-right" richColors />
  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  flex-direction: column;
  block-size: 100vh;
  background: var(--bg-color);
  color: var(--text-primary);
  overflow: hidden;
}

.top-nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
  padding: var(--space-sm) var(--space-xl);
  border-block-end: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--surface-color) 90%, transparent);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
}

.brand {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  min-inline-size: 0;
}

.brand-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  inline-size: 28px;
  block-size: 28px;
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--accent-secondary) 20%, transparent);
  border: 1px solid color-mix(in srgb, var(--accent-secondary) 35%, transparent);
  font-size: 16px;
}

.brand-title {
  font-size: var(--text-base);
  font-weight: 700;
  letter-spacing: 0.01em;
  white-space: nowrap;
}

.screen-nav {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
}

.nav-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  padding: 6px 12px;
  border-radius: var(--radius-full);
  border: 1px solid var(--border-color);
  background: var(--surface-color);
  color: var(--text-secondary);
  font-size: var(--text-sm);
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.nav-btn:hover {
  border-color: var(--border-color-hover);
  color: var(--text-primary);
}

.nav-btn.active {
  border-color: color-mix(in srgb, var(--accent-secondary) 60%, var(--border-color));
  background: color-mix(in srgb, var(--accent-secondary) 18%, var(--surface-color));
  color: var(--text-primary);
}

.nav-icon {
  line-height: 1;
}

.screen-content {
  flex: 1;
  min-block-size: 0;
}

.app-layout {
  display: grid;
  grid-template-columns: 240px 1fr 360px;
  block-size: 100%;
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
  min-block-size: 0;
  overflow: hidden;
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

.settings-screen {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--space-lg);
  block-size: 100%;
  overflow: auto;
  padding: var(--space-xl);
}

.settings-card {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  padding: var(--space-lg);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--surface-color);
}

.settings-card.full-width {
  grid-column: 1 / -1;
}

.settings-title,
.settings-subtitle {
  margin: 0;
  color: var(--text-primary);
}

.settings-title {
  font-size: var(--text-xl);
}

.settings-subtitle {
  font-size: var(--text-lg);
}

.settings-body {
  color: var(--text-secondary);
  line-height: 1.6;
}

.settings-list {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  margin: var(--space-sm) 0 0;
  padding: 0;
}

.settings-list-item {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--space-sm);
  padding: var(--space-sm) var(--space-md);
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-color);
  background: color-mix(in srgb, var(--bg-color) 45%, transparent);
}

.settings-item-label {
  font-weight: 600;
  color: var(--text-primary);
}

.settings-item-hint {
  color: var(--text-muted);
  font-size: var(--text-xs);
}

/* ── Responsive: collapse sidebar on narrow screens ──────────────────────── */
@media (max-width: 1024px) {
  .top-nav {
    align-items: flex-start;
    flex-direction: column;
    padding: var(--space-md);
  }

  .screen-nav {
    inline-size: 100%;
    overflow-x: auto;
    padding-block-end: 2px;
  }

  .nav-btn {
    flex: 0 0 auto;
  }

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

  .settings-screen {
    grid-template-columns: 1fr;
    padding: var(--space-md);
  }

  .settings-list-item {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
