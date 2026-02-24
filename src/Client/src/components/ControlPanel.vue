<script setup lang="ts">
import { onMounted } from "vue";
import { useObs } from "../composables/useObs";

const { connect, isConnected, switchToScene } = useObs();

onMounted(() => {
  // Attempt to connect to OBS WebSocket on mount
  // Make sure OBS WebSocket is enabled on port 4455 without a password,
  // or update the credentials here if needed.
  connect("ws://localhost:4455", "");
});

const playAds = () => {
  // Replace 'Ads' with the exact name of your OBS scene for ads
  switchToScene("BRB");
};
</script>

<template>
  <div class="control-panel">
    <div class="panel-header">
      <h2>Control Panel</h2>
      <div class="obs-status" :class="{ connected: isConnected }">
        <span class="status-indicator"></span>
        {{ isConnected ? "OBS Connected" : "OBS Disconnected" }}
      </div>
    </div>

    <div class="panel-actions">
      <button class="action-btn ads-btn" @click="playAds" :disabled="!isConnected" title="Switch OBS to Ads scene">
        <span class="icon">📺</span>
        Play Ads
      </button>
    </div>
  </div>
</template>

<style scoped>
.control-panel {
  display: flex;
  flex-direction: column;
  block-size: 100%;
  background: var(--surface-color);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: var(--space-md);
  gap: var(--space-md);
  box-shadow: 0 4px 6px -1px color-mix(in srgb, var(--bg-color) 50%, transparent);
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-block-end: 1px solid var(--border-color);
  padding-block-end: var(--space-sm);
}

h2 {
  margin: 0;
  font-size: var(--text-base);
  font-weight: 600;
  color: var(--text-primary);
}

.obs-status {
  display: flex;
  align-items: center;
  gap: var(--space-xs);
  font-size: var(--text-xs);
  color: var(--text-secondary);
  padding: var(--space-xs) var(--space-sm);
  border-radius: var(--radius-full);
  background: var(--surface-color-hover);
}

.status-indicator {
  inline-size: 8px;
  block-size: 8px;
  border-radius: var(--radius-full);
  background: var(--error-color);
  box-shadow: 0 0 5px var(--error-color);
}

.obs-status.connected .status-indicator {
  background: var(--success-color);
  box-shadow: 0 0 5px var(--success-color);
}

.panel-actions {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: var(--space-sm);
}

.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-sm);
  padding: var(--space-sm) var(--space-md);
  border: 1px solid transparent;
  border-radius: var(--radius-sm);
  font-size: var(--text-sm);
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-normal);
  font-family: inherit;
  color: var(--text-primary);
  background: var(--surface-color-hover);
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  filter: grayscale(1);
}

.action-btn:hover:not(:disabled) {
  background: var(--border-color);
}

.ads-btn {
  background: var(--accent-secondary);
  color: white;
}

.ads-btn:hover:not(:disabled) {
  background: var(--accent-primary-hover);
}

.icon {
  font-size: var(--text-base);
}
</style>
