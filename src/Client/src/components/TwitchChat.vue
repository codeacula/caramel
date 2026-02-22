<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import { useTwitchChat } from "../composables/useTwitchChat";
import type { TwitchChatMessage } from "../composables/useTwitchChat";

const { messages, status, errorMessage, clearMessages } = useTwitchChat();

const feedEl = ref<HTMLElement | null>(null);
const autoScroll = ref(true);

const statusLabel = computed(() => {
  switch (status.value) {
    case "connected":
      return "Live";
    case "connecting":
      return "Connecting…";
    case "disconnected":
      return "Disconnected";
    case "error":
      return "Error";
  }
});

const statusClass = computed(() => ({
  "status-badge": true,
  "status-live": status.value === "connected",
  "status-connecting": status.value === "connecting",
  "status-disconnected": status.value === "disconnected",
  "status-error": status.value === "error",
}));

function formatTime(timestamp: string): string {
  try {
    return new Date(timestamp).toLocaleTimeString(undefined, {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  } catch {
    return "";
  }
}

function usernameStyle(msg: TwitchChatMessage): string {
  return msg.color ? `color: ${msg.color}` : "";
}

function onScroll() {
  if (!feedEl.value) return;
  const el = feedEl.value;
  const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 40;
  autoScroll.value = atBottom;
}

watch(
  messages,
  () => {
    if (!autoScroll.value) return;
    nextTick(() => {
      if (feedEl.value) {
        feedEl.value.scrollTop = feedEl.value.scrollHeight;
      }
    });
  },
  { deep: false }
);

function scrollToBottom() {
  autoScroll.value = true;
  nextTick(() => {
    if (feedEl.value) {
      feedEl.value.scrollTop = feedEl.value.scrollHeight;
    }
  });
}
</script>

<template>
  <div class="chat-panel">
    <!-- Header -->
    <div class="chat-header">
      <div class="chat-title">
        <svg class="twitch-icon" viewBox="0 0 24 24" aria-hidden="true">
          <path
            d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714z"
          />
        </svg>
        <span>Twitch Chat</span>
      </div>

      <div class="chat-controls">
        <span :class="statusClass">
          <span class="status-dot" aria-hidden="true"></span>
          {{ statusLabel }}
        </span>

        <a href="/auth/login" class="btn btn-auth" target="_blank" rel="noopener noreferrer">
          Authorize Twitch
        </a>

        <button class="btn btn-clear" @click="clearMessages" title="Clear messages">
          Clear
        </button>
      </div>
    </div>

    <!-- Error banner -->
    <div v-if="errorMessage" class="error-banner" role="alert">
      ⚠ {{ errorMessage }}
    </div>

    <!-- Empty state -->
    <div v-if="messages.length === 0" class="empty-state">
      <p v-if="status === 'connecting'">Connecting to chat feed…</p>
      <p v-else-if="status === 'connected'">
        Waiting for messages. Make sure the bot is authorized and your channel IDs are configured.
      </p>
      <p v-else>
        No messages yet. Connect by authorizing Twitch above, then reload.
      </p>
    </div>

    <!-- Message feed -->
    <div
      v-else
      ref="feedEl"
      class="chat-feed"
      role="log"
      aria-live="polite"
      aria-label="Twitch chat messages"
      @scroll="onScroll"
    >
      <div
        v-for="msg in messages"
        :key="msg.messageId"
        class="chat-message"
      >
        <span class="msg-time">{{ formatTime(msg.timestamp) }}</span>
        <span
          class="msg-username"
          :style="usernameStyle(msg)"
          :title="`#${msg.broadcasterLogin}`"
        >{{ msg.chatterDisplayName }}</span>
        <span class="msg-colon" aria-hidden="true">:</span>
        <span class="msg-text">{{ msg.messageText }}</span>
      </div>
    </div>

    <!-- Scroll-to-bottom button -->
    <Transition name="fade">
      <button
        v-if="!autoScroll && messages.length > 0"
        class="scroll-btn"
        @click="scrollToBottom"
        aria-label="Scroll to latest messages"
      >
        ↓ Latest
      </button>
    </Transition>

    <!-- Footer -->
    <div class="chat-footer">
      {{ messages.length }} message{{ messages.length !== 1 ? "s" : "" }}
    </div>
  </div>
</template>

<style scoped>
/* Panel */
.chat-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  background: #0e0e10;
  color: #efeff1;
  font-family: "Inter", "Segoe UI", system-ui, sans-serif;
  font-size: 14px;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #2a2a2e;
}

/* Header */
.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 10px 14px;
  background: #18181b;
  border-bottom: 1px solid #2a2a2e;
  flex-shrink: 0;
  flex-wrap: wrap;
}

.chat-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
  color: #bf94ff;
}

.twitch-icon {
  width: 18px;
  height: 18px;
  fill: #bf94ff;
  flex-shrink: 0;
}

.chat-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

/* Status badge */
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 9px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.02em;
  white-space: nowrap;
}

.status-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-live {
  background: rgba(0, 200, 80, 0.15);
  color: #00c850;
  border: 1px solid rgba(0, 200, 80, 0.3);
}
.status-live .status-dot {
  background: #00c850;
  box-shadow: 0 0 5px #00c850;
  animation: pulse 1.8s ease-in-out infinite;
}

.status-connecting {
  background: rgba(255, 180, 0, 0.12);
  color: #ffb400;
  border: 1px solid rgba(255, 180, 0, 0.3);
}
.status-connecting .status-dot {
  background: #ffb400;
  animation: pulse 1s ease-in-out infinite;
}

.status-disconnected {
  background: rgba(150, 150, 160, 0.1);
  color: #909096;
  border: 1px solid rgba(150, 150, 160, 0.2);
}
.status-disconnected .status-dot {
  background: #909096;
}

.status-error {
  background: rgba(255, 80, 80, 0.12);
  color: #ff5050;
  border: 1px solid rgba(255, 80, 80, 0.25);
}
.status-error .status-dot {
  background: #ff5050;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.35; }
}

/* Buttons */
.btn {
  padding: 4px 12px;
  border-radius: 5px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  text-decoration: none;
  transition: background 0.15s, border-color 0.15s;
  white-space: nowrap;
}

.btn-auth {
  background: #9147ff;
  color: #fff;
  border-color: #9147ff;
}
.btn-auth:hover {
  background: #a970ff;
  border-color: #a970ff;
}

.btn-clear {
  background: transparent;
  color: #adadb8;
  border-color: #3a3a40;
}
.btn-clear:hover {
  background: #2a2a2e;
  color: #efeff1;
}

/* Error banner */
.error-banner {
  padding: 8px 14px;
  background: rgba(255, 80, 80, 0.1);
  color: #ff8080;
  border-bottom: 1px solid rgba(255, 80, 80, 0.2);
  font-size: 13px;
  flex-shrink: 0;
}

/* Empty state */
.empty-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 24px;
  text-align: center;
  color: #6b6b7d;
  font-size: 13px;
  line-height: 1.6;
}

/* Feed */
.chat-feed {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
  min-height: 0;
  scroll-behavior: smooth;
}

.chat-feed::-webkit-scrollbar {
  width: 6px;
}
.chat-feed::-webkit-scrollbar-track {
  background: transparent;
}
.chat-feed::-webkit-scrollbar-thumb {
  background: #3a3a40;
  border-radius: 3px;
}
.chat-feed::-webkit-scrollbar-thumb:hover {
  background: #52525e;
}

/* Individual message */
.chat-message {
  display: flex;
  align-items: baseline;
  gap: 0;
  padding: 3px 14px;
  line-height: 1.55;
  word-break: break-word;
  transition: background 0.1s;
}
.chat-message:hover {
  background: rgba(255, 255, 255, 0.04);
}

.msg-time {
  color: #6b6b7d;
  font-size: 11px;
  flex-shrink: 0;
  margin-right: 8px;
  font-variant-numeric: tabular-nums;
}

.msg-username {
  font-weight: 700;
  cursor: default;
  flex-shrink: 0;
}

.msg-colon {
  color: #adadb8;
  margin: 0 5px 0 1px;
  flex-shrink: 0;
}

.msg-text {
  color: #efeff1;
  flex: 1;
}

/* Scroll-to-bottom button */
.scroll-btn {
  position: absolute;
  bottom: 44px;
  left: 50%;
  transform: translateX(-50%);
  background: #9147ff;
  color: #fff;
  border: none;
  padding: 6px 16px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.5);
  transition: background 0.15s, transform 0.15s;
  z-index: 10;
}
.scroll-btn:hover {
  background: #a970ff;
  transform: translateX(-50%) translateY(-1px);
}

/* Footer */
.chat-footer {
  padding: 5px 14px;
  font-size: 11px;
  color: #6b6b7d;
  background: #18181b;
  border-top: 1px solid #2a2a2e;
  flex-shrink: 0;
  text-align: right;
}

/* Fade transition */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
