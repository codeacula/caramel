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
  height: 100%;
  background: #18181b;
  border: 1px solid #2a2a2e;
  border-radius: 8px;
  padding: 16px;
  gap: 16px;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #2a2a2e;
  padding-bottom: 12px;
}

h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #efeff1;
}

.obs-status {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: #adadb8;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #f87171; /* Red for disconnected */
}

.obs-status.connected .status-indicator {
  background: #4ade80; /* Green for connected */
}

.panel-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  font-family: inherit;
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.ads-btn {
  background: #9146ff;
  color: white;
}

.ads-btn:hover:not(:disabled) {
  background: #a970ff;
}

.icon {
  font-size: 16px;
}
</style>
