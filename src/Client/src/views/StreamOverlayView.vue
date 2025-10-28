<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue'

// Sprint and stream countdown placeholders
const sprintLeft = ref(25 * 60) // 25 minutes
const streamLeft = ref(2 * 60 + 43) // 02:43

const rotateItems = [
  'Stream Stats',
  'Game Stats',
  'Over Timers?'
]
const rotateIndex = ref(0)

let tickTimer: number | undefined
let rotateTimer: number | undefined

const formatTime = (totalSeconds: number) => {
  const mins = Math.floor(Math.max(totalSeconds, 0) / 60)
    .toString()
    .padStart(2, '0')
  const secs = Math.floor(Math.max(totalSeconds, 0) % 60)
    .toString()
    .padStart(2, '0')
  return `${mins}:${secs}`
}

const sprintTime = computed(() => formatTime(sprintLeft.value))
const streamTime = computed(() => formatTime(streamLeft.value))
const rotatingText = computed(() => rotateItems[rotateIndex.value])

onMounted(() => {
  tickTimer = window.setInterval(() => {
    if (sprintLeft.value > 0) sprintLeft.value -= 1
    if (streamLeft.value > 0) streamLeft.value -= 1
  }, 1000)

  rotateTimer = window.setInterval(() => {
    rotateIndex.value = (rotateIndex.value + 1) % rotateItems.length
  }, 4000)
})

onUnmounted(() => {
  if (tickTimer) window.clearInterval(tickTimer)
  if (rotateTimer) window.clearInterval(rotateTimer)
})
</script>

<template>
  <main class="overlay">
    <!-- Top row: Main content + Right sidebar -->
    <div class="top-row">
      <div class="main-content">
        <div class="content-backdrop">
          <!-- Main game/desktop capture area -->
        </div>
      </div>

      <aside class="sidebar">
        <div class="widget timer-widget">
          <div class="widget-content">
            <h2 class="widget-title">Sprint Timer</h2>
            <div class="timer-display">{{ sprintTime }}</div>
          </div>
        </div>

        <div class="widget alerts-widget">
          <div class="widget-content">
            <h3 class="widget-title">Alerts</h3>
            <div class="alerts-list">
              <div class="alert-item">New follower: <span class="highlight">Ada</span></div>
              <div class="alert-item">Sub from <span class="highlight">Beau</span> ×3</div>
              <div class="alert-item">Raid by <span class="highlight">Casey</span> (12)</div>
            </div>
          </div>
        </div>

        <div class="widget chat-widget">
          <div class="widget-content">
            <h3 class="widget-title">Chat</h3>
            <div class="chat-messages">
              <p><span class="highlight">you</span>: Welcome in!</p>
              <p><span class="highlight">bot</span>: Remember to hydrate 💧</p>
              <p class="muted">(Chat feed)</p>
            </div>
          </div>
        </div>
      </aside>
    </div>

    <!-- Bottom row: Info cards with space for camera -->
    <div class="bottom-row">
      <div class="info-cards">
        <div class="card stream-card">
          <div class="card-content">
            <h3 class="card-title">Stream Title</h3>
            <p>Stream Goals</p>
            <p class="muted">Ending in {{ streamTime }}</p>
          </div>
        </div>

        <div class="card sprint-card">
          <div class="card-content">
            <h3 class="card-title">Current Sprint</h3>
            <p>Finish feature X, write tests</p>
          </div>
        </div>

        <div class="card rotating-card">
          <div class="card-content">
            <h3 class="card-title">Info</h3>
            <p>{{ rotatingText }}</p>
          </div>
        </div>
      </div>

      <!-- Camera space -->
      <div class="camera-space">
        <div class="camera-placeholder">
          <!-- Webcam feed will go here -->
        </div>
      </div>
    </div>
  </main>
</template>

<style scoped>
/* Original Blade Runner / bisexual lighting aesthetic */
.overlay {
  position: relative;
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  padding: 20px;
  gap: 20px;
  box-sizing: border-box;
  color: #e9ecff;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;

  /* Deep atmospheric background with pink/blue light sources */
  background:
    radial-gradient(ellipse 120% 80% at 10% 20%, rgba(255, 0, 128, 0.15), transparent 50%),
    radial-gradient(ellipse 100% 70% at 90% 80%, rgba(0, 180, 255, 0.12), transparent 50%),
    radial-gradient(ellipse 80% 60% at 50% 50%, rgba(138, 43, 226, 0.08), transparent 60%),
    linear-gradient(180deg, #0a0a12 0%, #050508 100%);
}

/* Top row: Main content area + sidebar */
.top-row {
  display: flex;
  gap: 20px;
  flex: 1;
  min-height: 0;
}

.main-content {
  flex: 1;
  position: relative;
  overflow: hidden;
}

.content-backdrop {
  width: 100%;
  height: 100%;
  background:
    linear-gradient(135deg, rgba(255, 0, 128, 0.03), rgba(0, 180, 255, 0.03)),
    rgba(8, 8, 16, 0.6);
  backdrop-filter: blur(40px) saturate(1.4);
  box-shadow:
    0 0 80px rgba(255, 0, 128, 0.15),
    0 0 120px rgba(0, 180, 255, 0.1),
    inset 0 0 100px rgba(138, 43, 226, 0.05);
}

/* Right sidebar */
.sidebar {
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.widget {
  background: linear-gradient(135deg, rgba(20, 18, 35, 0.7), rgba(15, 15, 30, 0.8));
  backdrop-filter: blur(20px) saturate(1.2);
  padding: 16px;
  position: relative;
  overflow: hidden;
}

/* Atmospheric glow effects for widgets */
.widget::before {
  content: '';
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: radial-gradient(circle, rgba(255, 0, 128, 0.08), transparent 60%);
  pointer-events: none;
}

.timer-widget {
  min-height: 100px;
  box-shadow: 0 8px 32px rgba(255, 0, 128, 0.2);
}

.timer-widget::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg,
    transparent,
    rgba(255, 0, 128, 0.8) 50%,
    transparent
  );
  animation: shimmer 3s infinite;
  pointer-events: none;
}

@keyframes shimmer {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 1; }
}

.alerts-widget {
  flex: 1;
  min-height: 160px;
  box-shadow: 0 8px 32px rgba(138, 43, 226, 0.15);
}

.chat-widget {
  flex: 1;
  min-height: 200px;
  box-shadow: 0 8px 32px rgba(0, 180, 255, 0.2);
}

.chat-widget::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg,
    transparent,
    rgba(0, 180, 255, 0.8) 50%,
    transparent
  );
  animation: shimmer 4s infinite;
  pointer-events: none;
}

.widget-content {
  position: relative;
  z-index: 1;
}

.widget-title {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: rgba(255, 255, 255, 0.6);
  margin: 0 0 12px 0;
  font-weight: 600;
}

.timer-display {
  font-size: 48px;
  font-weight: 700;
  letter-spacing: 4px;
  background: linear-gradient(135deg, #ff0080, #ff66b3);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  text-shadow: 0 0 40px rgba(255, 0, 128, 0.5);
}

.alerts-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.alert-item {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.85);
  padding: 4px 0;
}

.chat-messages {
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 14px;
  max-height: 100%;
  overflow-y: auto;
}

.chat-messages p {
  margin: 0;
  color: rgba(255, 255, 255, 0.85);
}

.highlight {
  color: #00b4ff;
  font-weight: 600;
}

.muted {
  color: rgba(255, 255, 255, 0.4);
  font-style: italic;
}

/* Bottom row: Info cards + camera space */
.bottom-row {
  display: flex;
  gap: 20px;
  height: 140px;
}

.info-cards {
  flex: 1;
  display: flex;
  gap: 16px;
}

.card {
  flex: 1;
  background: linear-gradient(135deg, rgba(20, 18, 35, 0.7), rgba(15, 15, 30, 0.8));
  backdrop-filter: blur(20px) saturate(1.2);
  padding: 16px;
  position: relative;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(138, 43, 226, 0.12);
}

.card::before {
  content: '';
  position: absolute;
  top: -100%;
  left: -100%;
  width: 300%;
  height: 300%;
  background: radial-gradient(circle, rgba(138, 43, 226, 0.08), transparent 50%);
  pointer-events: none;
}

.stream-card {
  box-shadow: 0 8px 32px rgba(255, 0, 128, 0.15);
}

.sprint-card {
  box-shadow: 0 8px 32px rgba(138, 43, 226, 0.15);
}

.rotating-card {
  box-shadow: 0 8px 32px rgba(0, 180, 255, 0.15);
}

.card-content {
  position: relative;
  z-index: 1;
}

.card-title {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: rgba(255, 255, 255, 0.6);
  margin: 0 0 8px 0;
  font-weight: 600;
}

.card-content p {
  margin: 4px 0;
  font-size: 14px;
  color: rgba(255, 255, 255, 0.85);
}

/* Camera space */
.camera-space {
  width: 240px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.camera-placeholder {
  width: 200px;
  height: 200px;
  border-radius: 50%;
  background:
    radial-gradient(circle at 30% 30%, rgba(255, 0, 128, 0.2), transparent 60%),
    radial-gradient(circle at 70% 70%, rgba(0, 180, 255, 0.15), transparent 60%),
    linear-gradient(135deg, rgba(20, 18, 35, 0.8), rgba(15, 15, 30, 0.9));
  backdrop-filter: blur(30px);
  box-shadow:
    0 0 60px rgba(255, 0, 128, 0.3),
    0 0 100px rgba(0, 180, 255, 0.2),
    inset 0 0 50px rgba(138, 43, 226, 0.1);
  position: relative;
}

.camera-placeholder::after {
  content: '';
  position: absolute;
  inset: 20px;
  border-radius: 50%;
  background: radial-gradient(
    circle,
    rgba(255, 255, 255, 0.03),
    transparent 70%
  );
}

/* Responsiveness */
@media (max-width: 1200px) {
  .top-row {
    flex-direction: column;
  }

  .sidebar {
    width: 100%;
    flex-direction: row;
    flex-wrap: wrap;
  }

  .widget {
    flex: 1;
    min-width: 200px;
  }

  .bottom-row {
    flex-direction: column;
    height: auto;
  }

  .info-cards {
    flex-direction: column;
  }

  .camera-space {
    width: 100%;
    height: 240px;
  }
}
</style>
