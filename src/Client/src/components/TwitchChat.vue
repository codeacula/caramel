<script setup lang="ts">
import { computed, nextTick, ref, watch } from "vue";
import { useTwitchChat } from "../composables/useTwitchChat";
import { useTwitchSetup } from "../composables/useTwitchSetup";
import type { TwitchChatMessage } from "../composables/useTwitchChat";

const {
  feedItems,
  status,
  errorMessage,
  sendStatus,
  sendError,
  isAuthorized,
  isSetupConfigured,
  clearMessages,
  clearSendError,
  sendMessage,
  maxMessageLength,
} = useTwitchChat();

// ── Setup wizard ─────────────────────────────────────────────────────────────

const { requestStatus: setupRequestStatus, errorMessage: setupError, submitSetup } = useTwitchSetup();

const setupBotLogin = ref("");
const setupChannelLogins = ref("");
const setupSaving = computed(() => setupRequestStatus.value === "saving");

async function handleSetupSubmit() {
  const botLogin = setupBotLogin.value.trim();
  const channelLogins = setupChannelLogins.value
    .split(",")
    .map((s) => s.trim())
    .filter((s) => s.length > 0);

  if (!botLogin || channelLogins.length === 0) return;

  await submitSetup(botLogin, channelLogins);
  // isSetupConfigured will be updated via the WebSocket setup_status push
}

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
  feedItems,
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
  <aside class="chat-panel">
    <!-- Header -->
    <header class="chat-header">
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

        <a v-if="!isAuthorized" href="/auth/twitch/login" class="btn btn-auth" target="_blank" rel="noopener noreferrer">
          Authorize Twitch
        </a>

        <button class="btn btn-clear" @click="clearMessages" title="Clear messages">Clear</button>
      </div>
    </header>

    <!-- Setup wizard (shown when not yet configured) -->
    <div v-if="!isSetupConfigured" class="setup-panel">
      <h2 class="setup-title">Twitch Setup</h2>
      <p class="setup-desc">
        Enter your bot's username and the channel(s) to monitor. Separate multiple channels with commas.
      </p>

      <div v-if="setupError" class="error-banner" role="alert">⚠ {{ setupError }}</div>

      <form class="setup-form" @submit.prevent="handleSetupSubmit">
        <label class="setup-label" for="setup-bot-login">Bot username</label>
        <input
          id="setup-bot-login"
          v-model="setupBotLogin"
          class="setup-input"
          type="text"
          placeholder="mybot"
          :disabled="setupSaving"
          required
        />

        <label class="setup-label" for="setup-channels">Channel(s)</label>
        <input
          id="setup-channels"
          v-model="setupChannelLogins"
          class="setup-input"
          type="text"
          placeholder="channel1, channel2"
          :disabled="setupSaving"
          required
        />

        <button class="btn btn-auth setup-submit" type="submit" :disabled="setupSaving || !setupBotLogin.trim() || !setupChannelLogins.trim()">
          <span v-if="setupSaving">Saving…</span>
          <span v-else>Save Setup</span>
        </button>
      </form>
    </div>

    <!-- WebSocket error banner -->
    <div v-if="isSetupConfigured && errorMessage" class="error-banner" role="alert">⚠ {{ errorMessage }}</div>

    <!-- Empty state -->
    <div v-if="isSetupConfigured && feedItems.length === 0" class="empty-state">
      <p v-if="status === 'connecting'">Connecting to chat feed…</p>
      <p v-else-if="status === 'connected'">
        Waiting for messages. Make sure the bot is authorized and your channel IDs are configured.
      </p>
      <p v-else>No messages yet. Connect by authorizing Twitch above, then reload.</p>
    </div>

    <!-- Message feed -->
    <div
      v-if="isSetupConfigured && feedItems.length > 0"
      ref="feedEl"
      class="chat-feed"
      role="log"
      aria-live="polite"
      aria-label="Twitch chat feed"
      @scroll="onScroll"
    >
      <template v-for="item in feedItems" :key="item.kind === 'chat' ? item.data.messageId : item.data.redemptionId">
        <!-- Chat message row -->
        <div v-if="item.kind === 'chat'" class="chat-message">
          <span class="msg-time">{{ formatTime(item.data.timestamp) }}</span>
          <span class="msg-username" :style="usernameStyle(item.data)" :title="`#${item.data.broadcasterLogin}`">{{
            item.data.chatterDisplayName
          }}</span>
          <span class="msg-colon" aria-hidden="true">:</span>
          <span class="msg-text">{{ item.data.messageText }}</span>
        </div>

        <!-- Channel point redeem row -->
        <div v-else-if="item.kind === 'redeem'" class="chat-redeem">
          <span class="msg-time">{{ formatTime(item.data.redeemedAt) }}</span>
          <svg class="redeem-icon" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 14H9V8h2v8zm4 0h-2V8h2v8z" />
          </svg>
          <span class="redeem-username">{{ item.data.redeemerDisplayName }}</span>
          <span class="redeem-sep" aria-hidden="true">redeemed</span>
          <span class="redeem-title">{{ item.data.rewardTitle }}</span>
          <span v-if="item.data.userInput" class="redeem-input">— {{ item.data.userInput }}</span>
          <span class="redeem-cost">{{ item.data.rewardCost.toLocaleString() }} pts</span>
        </div>
      </template>
    </div>

    <!-- Scroll-to-bottom button -->
    <Transition name="fade">
      <button
        v-if="!autoScroll && feedItems.length > 0"
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
    <footer class="chat-footer">{{ feedItems.length }} event{{ feedItems.length !== 1 ? "s" : "" }}</footer>
  </aside>
</template>

<style scoped>
/* ── Panel ───────────────────────────────────────────────────────────────── */
.chat-panel {
  display: flex;
  flex-direction: column;
  block-size: 100%;
  min-block-size: 0;
  background: var(--bg-color);
  color: var(--text-primary);
  font-family: var(--font-sans);
  font-size: var(--text-sm);
  border-radius: var(--radius-md);
  overflow: hidden;
  border: 1px solid var(--border-color);
  position: relative;
}

/* ── Header ──────────────────────────────────────────────────────────────── */
.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
  padding: var(--space-sm) var(--space-md);
  background: color-mix(in srgb, var(--surface-color) 85%, transparent);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-block-end: 1px solid var(--border-color);
  flex-shrink: 0;
  flex-wrap: wrap;
}

.chat-title {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  font-weight: 600;
  font-size: var(--text-base);
  color: var(--accent-primary);
}

.twitch-icon {
  inline-size: 18px;
  block-size: 18px;
  fill: var(--accent-primary);
  flex-shrink: 0;
}

.chat-controls {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  flex-wrap: wrap;
}

/* ── Status badge ────────────────────────────────────────────────────────── */
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  padding: var(--space-xs) var(--space-sm);
  border-radius: var(--radius-full);
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.02em;
  white-space: nowrap;
}

.status-dot {
  inline-size: 7px;
  block-size: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-live {
  background: var(--success-bg);
  color: var(--success-color);
  border: 1px solid color-mix(in srgb, var(--success-color) 30%, transparent);
}
.status-live .status-dot {
  background: var(--success-color);
  box-shadow: 0 0 5px var(--success-color);
  animation: pulse 1.8s ease-in-out infinite;
}

.status-connecting {
  background: var(--warning-bg);
  color: var(--warning-color);
  border: 1px solid color-mix(in srgb, var(--warning-color) 30%, transparent);
}
.status-connecting .status-dot {
  background: var(--warning-color);
  animation: pulse 1s ease-in-out infinite;
}

.status-disconnected {
  background: var(--info-bg);
  color: var(--info-color);
  border: 1px solid color-mix(in srgb, var(--info-color) 20%, transparent);
}
.status-disconnected .status-dot {
  background: var(--info-color);
}

.status-error {
  background: var(--error-bg);
  color: var(--error-color);
  border: 1px solid color-mix(in srgb, var(--error-color) 25%, transparent);
}
.status-error .status-dot {
  background: var(--error-color);
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
  padding: var(--space-xs) var(--space-sm);
  border-radius: var(--radius-sm);
  font-size: var(--text-xs);
  font-weight: 600;
  cursor: pointer;
  border: 1px solid transparent;
  text-decoration: none;
  transition: background var(--transition-fast), border-color var(--transition-fast);
  white-space: nowrap;
}

.btn-auth {
  background: var(--accent-secondary);
  color: #fff;
  border-color: var(--accent-secondary);
}
.btn-auth:hover {
  background: var(--accent-primary-hover);
  border-color: var(--accent-primary-hover);
}

.btn-clear {
  background: transparent;
  color: var(--text-secondary);
  border-color: var(--border-color-hover);
}
.btn-clear:hover {
  background: var(--border-color);
  color: var(--text-primary);
}

/* ── Error banners ───────────────────────────────────────────────────────── */
.error-banner {
  padding: var(--space-sm) var(--space-md);
  background: var(--error-bg);
  color: var(--error-color);
  border-block-end: 1px solid color-mix(in srgb, var(--error-color) 25%, transparent);
  font-size: var(--text-sm);
  flex-shrink: 0;
}

.send-error-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  padding: var(--space-sm) var(--space-md);
  background: var(--warning-bg);
  color: var(--warning-color);
  border-block-start: 1px solid color-mix(in srgb, var(--warning-color) 20%, transparent);
  font-size: var(--text-xs);
  flex-shrink: 0;
}

.send-error-dismiss {
  background: transparent;
  border: none;
  color: var(--warning-color);
  cursor: pointer;
  font-size: var(--text-sm);
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
  padding: var(--space-xl) var(--space-xl);
  text-align: center;
  color: var(--text-muted);
  font-size: var(--text-sm);
  line-height: 1.6;
}

/* ── Feed ────────────────────────────────────────────────────────────────── */
.chat-feed {
  flex: 1;
  overflow-y: auto;
  padding: var(--space-sm) 0;
  min-block-size: 0;
  scroll-behavior: smooth;
}

.chat-feed::-webkit-scrollbar {
  inline-size: 6px;
}
.chat-feed::-webkit-scrollbar-track {
  background: transparent;
}
.chat-feed::-webkit-scrollbar-thumb {
  background: var(--border-color-hover);
  border-radius: var(--radius-sm);
}
.chat-feed::-webkit-scrollbar-thumb:hover {
  background: var(--text-muted);
}

/* ── Message rows ────────────────────────────────────────────────────────── */
.chat-message {
  display: flex;
  align-items: baseline;
  padding: var(--space-xs) var(--space-md);
  line-height: 1.55;
  word-break: break-word;
  transition: background var(--transition-fast);
}
.chat-message:hover {
  background: color-mix(in srgb, var(--text-primary) 4%, transparent);
}

/* ── Redeem rows ─────────────────────────────────────────────────────────── */
.chat-redeem {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 0 5px;
  padding: var(--space-xs) var(--space-md);
  line-height: 1.55;
  word-break: break-word;
  background: color-mix(in srgb, var(--accent-secondary) 8%, transparent);
  border-inline-start: 2px solid var(--accent-secondary);
  transition: background var(--transition-fast);
}
.chat-redeem:hover {
  background: color-mix(in srgb, var(--accent-secondary) 14%, transparent);
}

.redeem-icon {
  inline-size: 13px;
  block-size: 13px;
  fill: var(--accent-secondary);
  flex-shrink: 0;
  align-self: center;
}

.redeem-username {
  font-weight: 700;
  color: var(--accent-secondary);
  flex-shrink: 0;
}

.redeem-sep {
  color: var(--text-muted);
  font-size: var(--text-xs);
  flex-shrink: 0;
}

.redeem-title {
  font-weight: 600;
  color: var(--text-primary);
  flex-shrink: 0;
}

.redeem-input {
  color: var(--text-secondary);
  font-style: italic;
  flex: 1;
  min-inline-size: 0;
}

.redeem-cost {
  margin-inline-start: auto;
  color: var(--accent-secondary);
  font-size: var(--text-xs);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  flex-shrink: 0;
  white-space: nowrap;
}

.msg-time {
  color: var(--text-muted);
  font-size: clamp(0.65rem, 0.7vw, 0.75rem);
  flex-shrink: 0;
  margin-inline-end: 8px;
  font-variant-numeric: tabular-nums;
}

.msg-username {
  font-weight: 700;
  cursor: default;
  flex-shrink: 0;
}

.msg-colon {
  color: var(--text-secondary);
  margin: 0 5px 0 1px;
  flex-shrink: 0;
}

.msg-text {
  color: var(--text-primary);
  flex: 1;
}

/* ── Scroll-to-bottom ────────────────────────────────────────────────────── */
.scroll-btn {
  position: absolute;
  bottom: 100px;
  left: 50%;
  transform: translateX(-50%);
  background: var(--accent-secondary);
  color: #fff;
  border: none;
  padding: 6px 16px;
  border-radius: var(--radius-full);
  font-size: var(--text-xs);
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.5);
  transition:
    background 0.15s,
    transform 0.15s;
  z-index: 10;
}
.scroll-btn:hover {
  background: var(--accent-primary-hover);
  transform: translateX(-50%) translateY(-1px);
}

/* ── Send bar ────────────────────────────────────────────────────────────── */
.send-bar {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  padding: var(--space-sm) 10px;
  background: var(--surface-color);
  border-block-start: 1px solid var(--border-color);
  flex-shrink: 0;
}

.send-input-wrap {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
}

.send-input {
  inline-size: 100%;
  padding: 7px 44px 7px 12px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-color-hover);
  background: var(--bg-color);
  color: var(--text-primary);
  font-size: var(--text-sm);
  font-family: inherit;
  outline: none;
  transition: border-color var(--transition-fast);
}
.send-input::placeholder {
  color: var(--text-muted);
}
.send-input:focus {
  border-color: var(--accent-secondary);
}
.send-input:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.char-count {
  position: absolute;
  right: 10px;
  font-size: clamp(0.65rem, 0.7vw, 0.75rem);
  font-variant-numeric: tabular-nums;
  color: var(--text-muted);
  pointer-events: none;
  user-select: none;
  transition: color 0.15s;
}
.char-warn {
  color: var(--warning-color);
}
.char-over {
  color: var(--error-color);
}

.send-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  inline-size: 36px;
  block-size: 36px;
  flex-shrink: 0;
  border-radius: var(--radius-sm);
  border: none;
  background: var(--accent-secondary);
  color: #fff;
  cursor: pointer;
  transition:
    background 0.15s,
    opacity 0.15s,
    transform 0.1s;
}
.send-btn:hover:not(:disabled) {
  background: var(--accent-primary-hover);
  transform: scale(1.05);
}
.send-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
  transform: none;
}

.send-icon {
  inline-size: 18px;
  block-size: 18px;
  fill: currentColor;
}

/* Spinner for the sending state */
.send-spinner {
  display: inline-block;
  inline-size: 16px;
  block-size: 16px;
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
  font-size: clamp(0.65rem, 0.7vw, 0.75rem);
  color: var(--text-muted);
  background: var(--surface-color);
  border-block-start: 1px solid var(--border-color);
  flex-shrink: 0;
  text-align: right;
}

/* ── Setup wizard ────────────────────────────────────────────────────────── */
.setup-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--space-xl) var(--space-xl);
  gap: var(--space-md);
}

.setup-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.setup-desc {
  font-size: var(--text-sm);
  color: var(--text-muted);
  text-align: center;
  margin: 0;
  max-inline-size: 360px;
  line-height: 1.5;
}

.setup-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  inline-size: 100%;
  max-inline-size: 360px;
}

.setup-label {
  font-size: var(--text-xs);
  font-weight: 600;
  color: var(--text-secondary);
}

.setup-input {
  padding: var(--space-sm) var(--space-md);
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-color-hover);
  background: var(--bg-color);
  color: var(--text-primary);
  font-size: var(--text-sm);
  font-family: inherit;
  outline: none;
  transition: border-color var(--transition-fast);
}
.setup-input::placeholder {
  color: var(--text-muted);
}
.setup-input:focus {
  border-color: var(--accent-secondary);
}
.setup-input:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.setup-submit {
  margin-block-start: 4px;
  padding: var(--space-sm) 16px;
  font-size: var(--text-sm);
  align-self: flex-end;
}
.setup-submit:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

/* ── Transitions ─────────────────────────────────────────────────────────── */
.fade-enter-active,
.fade-leave-active {
  transition: opacity var(--transition-normal);
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
