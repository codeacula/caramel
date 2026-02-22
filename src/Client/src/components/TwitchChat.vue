<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import { useTwitchChat } from "../composables/useTwitchChat";
import type { TwitchChatMessage } from "../composables/useTwitchChat";

const {
  messages,
  status,
  errorMessage,
  sendStatus,
  sendError,
  clearMessages,
  clearSendError,
  sendMessage,
  maxMessageLength,
} = useTwitchChat();

const feedEl = ref<HTMLElement | null>(null);
const autoScroll = ref(true);
const inputText = ref("");
const inputEl = ref<HTMLInputElement | null>(null);

// ── Connection status display ────────────────────────────────────────────────

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

// ── Send bar ─────────────────────────────────────────────────────────────────

const canSend = computed(
  () =>
    status.value === "connected" &&
    sendStatus.value !== "sending" &&
    inputText.value.trim().length > 0 &&
    inputText.value.trim().length <= maxMessageLength,
);

const charsLeft = computed(() => maxMessageLength - inputText.value.length);

const charCountClass = computed(() => ({
  "char-count": true,
  "char-warn": charsLeft.value <= 50 && charsLeft.value > 0,
  "char-over": charsLeft.value < 0,
}));

async function handleSend() {
  if (!canSend.value) return;
  const text = inputText.value;
  const ok = await sendMessage(text);
  if (ok) {
    inputText.value = "";
    inputEl.value?.focus();
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === "Enter" && !e.shiftKey) {
    e.preventDefault();
    handleSend();
  }
  // Clear a stale send error as soon as the user starts typing again
  if (sendError.value) clearSendError();
}

// ── Feed scroll ───────────────────────────────────────────────────────────────

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
      if (feedEl.value) feedEl.value.scrollTop = feedEl.value.scrollHeight;
    });
  },
  { deep: false },
);

function scrollToBottom() {
  autoScroll.value = true;
  nextTick(() => {
    if (feedEl.value) feedEl.value.scrollTop = feedEl.value.scrollHeight;
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

        <a href="/auth/login" class="btn btn-auth" target="_blank" rel="noopener noreferrer"> Authorize Twitch </a>

        <button class="btn btn-clear" @click="clearMessages" title="Clear messages">Clear</button>
      </div>
    </div>

    <!-- WebSocket error banner -->
    <div v-if="errorMessage" class="error-banner" role="alert">⚠ {{ errorMessage }}</div>

    <!-- Empty state -->
    <div v-if="messages.length === 0" class="empty-state">
      <p v-if="status === 'connecting'">Connecting to chat feed…</p>
      <p v-else-if="status === 'connected'">
        Waiting for messages. Make sure the bot is authorized and your channel IDs are configured.
      </p>
      <p v-else>No messages yet. Connect by authorizing Twitch above, then reload.</p>
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
      <div v-for="msg in messages" :key="msg.messageId" class="chat-message">
        <span class="msg-time">{{ formatTime(msg.timestamp) }}</span>
        <span class="msg-username" :style="usernameStyle(msg)" :title="`#${msg.broadcasterLogin}`">{{
          msg.chatterDisplayName
        }}</span>
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

    <!-- Send error banner -->
    <Transition name="fade">
      <div v-if="sendError" class="send-error-banner" role="alert">
        <span>⚠ {{ sendError }}</span>
        <button class="send-error-dismiss" @click="clearSendError" aria-label="Dismiss error">✕</button>
      </div>
    </Transition>

    <!-- Send bar -->
    <div class="send-bar">
      <div class="send-input-wrap">
        <input
          ref="inputEl"
          v-model="inputText"
          class="send-input"
          type="text"
          placeholder="Send a message…"
          :disabled="status !== 'connected' || sendStatus === 'sending'"
          :maxlength="maxMessageLength + 50"
          aria-label="Chat message"
          @keydown="handleKeydown"
        />
        <span :class="charCountClass" aria-live="polite">{{ charsLeft }}</span>
      </div>

      <button
        class="send-btn"
        :disabled="!canSend"
        :aria-busy="sendStatus === 'sending'"
        aria-label="Send message"
        @click="handleSend"
      >
        <span v-if="sendStatus === 'sending'" class="send-spinner" aria-hidden="true" />
        <svg v-else viewBox="0 0 24 24" aria-hidden="true" class="send-icon">
          <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z" />
        </svg>
      </button>
    </div>

    <!-- Footer -->
    <div class="chat-footer">{{ messages.length }} message{{ messages.length !== 1 ? "s" : "" }}</div>
  </div>
</template>

<style scoped>
/* ── Panel ───────────────────────────────────────────────────────────────── */
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
  position: relative;
}

/* ── Header ──────────────────────────────────────────────────────────────── */
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

/* ── Status badge ────────────────────────────────────────────────────────── */
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
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.35;
  }
}

/* ── Buttons ─────────────────────────────────────────────────────────────── */
.btn {
  padding: 4px 12px;
  border-radius: 5px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  text-decoration: none;
  transition:
    background 0.15s,
    border-color 0.15s;
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

/* ── Error banners ───────────────────────────────────────────────────────── */
.error-banner {
  padding: 8px 14px;
  background: rgba(255, 80, 80, 0.1);
  color: #ff8080;
  border-bottom: 1px solid rgba(255, 80, 80, 0.2);
  font-size: 13px;
  flex-shrink: 0;
}

.send-error-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 6px 14px;
  background: rgba(255, 140, 0, 0.1);
  color: #ffaa40;
  border-top: 1px solid rgba(255, 140, 0, 0.2);
  font-size: 12px;
  flex-shrink: 0;
}

.send-error-dismiss {
  background: transparent;
  border: none;
  color: #ffaa40;
  cursor: pointer;
  font-size: 13px;
  line-height: 1;
  padding: 0 2px;
  flex-shrink: 0;
  opacity: 0.7;
  transition: opacity 0.15s;
}
.send-error-dismiss:hover {
  opacity: 1;
}

/* ── Empty state ─────────────────────────────────────────────────────────── */
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

/* ── Feed ────────────────────────────────────────────────────────────────── */
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

/* ── Message rows ────────────────────────────────────────────────────────── */
.chat-message {
  display: flex;
  align-items: baseline;
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

/* ── Scroll-to-bottom ────────────────────────────────────────────────────── */
.scroll-btn {
  position: absolute;
  bottom: 100px;
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
  transition:
    background 0.15s,
    transform 0.15s;
  z-index: 10;
}
.scroll-btn:hover {
  background: #a970ff;
  transform: translateX(-50%) translateY(-1px);
}

/* ── Send bar ────────────────────────────────────────────────────────────── */
.send-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  background: #18181b;
  border-top: 1px solid #2a2a2e;
  flex-shrink: 0;
}

.send-input-wrap {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
}

.send-input {
  width: 100%;
  padding: 7px 44px 7px 12px;
  border-radius: 6px;
  border: 1px solid #3a3a40;
  background: #0e0e10;
  color: #efeff1;
  font-size: 13px;
  font-family: inherit;
  outline: none;
  transition: border-color 0.15s;
}
.send-input::placeholder {
  color: #6b6b7d;
}
.send-input:focus {
  border-color: #9147ff;
}
.send-input:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.char-count {
  position: absolute;
  right: 10px;
  font-size: 11px;
  font-variant-numeric: tabular-nums;
  color: #6b6b7d;
  pointer-events: none;
  user-select: none;
  transition: color 0.15s;
}
.char-warn {
  color: #ffb400;
}
.char-over {
  color: #ff5050;
}

.send-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  flex-shrink: 0;
  border-radius: 6px;
  border: none;
  background: #9147ff;
  color: #fff;
  cursor: pointer;
  transition:
    background 0.15s,
    opacity 0.15s,
    transform 0.1s;
}
.send-btn:hover:not(:disabled) {
  background: #a970ff;
  transform: scale(1.05);
}
.send-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
  transform: none;
}

.send-icon {
  width: 18px;
  height: 18px;
  fill: currentColor;
}

/* Spinner for the sending state */
.send-spinner {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* ── Footer ──────────────────────────────────────────────────────────────── */
.chat-footer {
  padding: 4px 14px;
  font-size: 11px;
  color: #6b6b7d;
  background: #18181b;
  border-top: 1px solid #2a2a2e;
  flex-shrink: 0;
  text-align: right;
}

/* ── Transitions ─────────────────────────────────────────────────────────── */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
