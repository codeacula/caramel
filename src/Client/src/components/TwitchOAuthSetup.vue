<script setup lang="ts">
import { ref, computed, onMounted } from "vue";

export interface TwitchAccountTokens {
  userId: string;
  login: string;
  hasRefreshToken: boolean;
  expiresAtTicks: number;
}

export interface TwitchSetupData {
  botLogin: string | null;
  botTokens: TwitchAccountTokens | null;
  broadcasterTokens: TwitchAccountTokens | null;
}

type OAuthStep = "idle" | "loading" | "awaiting-callback" | "success" | "error";

const setupData = ref<TwitchSetupData | null>(null);
const botOAuthStep = ref<OAuthStep>("idle");
const broadcasterOAuthStep = ref<OAuthStep>("idle");
const errorMessage = ref<string | null>(null);
const successMessage = ref<string | null>(null);

// Computed properties for status display
const botConfigured = computed(
  () => setupData.value?.botTokens !== null && setupData.value?.botTokens !== undefined
);
const broadcasterConfigured = computed(
  () =>
    setupData.value?.broadcasterTokens !== null &&
    setupData.value?.broadcasterTokens !== undefined
);

// Token expiration display
const formatExpiration = (expiresAtTicks: number): string => {
  if (!expiresAtTicks) return "Unknown";
  try {
    const date = new Date(expiresAtTicks / 10000);
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return "Invalid date";
  }
};

// Load setup status from gRPC endpoint
async function loadSetupStatus(): Promise<void> {
  try {
    const response = await fetch("/twitch/setup");
    if (response.ok) {
      const data = await response.json();
      setupData.value = data;
      successMessage.value = null;
      errorMessage.value = null;
    } else {
      errorMessage.value = `Failed to load setup status: ${response.status}`;
    }
  } catch (err) {
    errorMessage.value = `Failed to load setup: ${err instanceof Error ? err.message : "Unknown error"}`;
  }
}

// Initiate bot OAuth flow
function startBotOAuth(): void {
  botOAuthStep.value = "loading";
  errorMessage.value = null;

  // Store the return URL so we can redirect back after OAuth
  sessionStorage.setItem("oauth-return-url", window.location.href);
  sessionStorage.setItem("oauth-account-type", "bot");

  // Redirect to bot OAuth endpoint
  window.location.href = "/auth/twitch/login/bot";
}

// Initiate broadcaster OAuth flow
function startBroadcasterOAuth(): void {
  broadcasterOAuthStep.value = "loading";
  errorMessage.value = null;

  // Store the return URL so we can redirect back after OAuth
  sessionStorage.setItem("oauth-return-url", window.location.href);
  sessionStorage.setItem("oauth-account-type", "broadcaster");

  // Redirect to broadcaster OAuth endpoint
  window.location.href = "/auth/twitch/login/broadcaster";
}

// Handle OAuth callback (called when user returns from Twitch OAuth)
async function handleOAuthCallback(): Promise<void> {
  const accountType = sessionStorage.getItem("oauth-account-type");
  const returnUrl = sessionStorage.getItem("oauth-return-url");

  if (!accountType) return;

  const oAuthStepRef = accountType === "bot" ? botOAuthStep : broadcasterOAuthStep;

  oAuthStepRef.value = "awaiting-callback";

  // Wait a moment for the backend to process the token
  await new Promise((resolve) => setTimeout(resolve, 1000));

  // Reload setup status
  await loadSetupStatus();

  // Check if the appropriate tokens are now configured
  const isConfigured =
    accountType === "bot"
      ? setupData.value?.botTokens !== null
      : setupData.value?.broadcasterTokens !== null;

   if (isConfigured) {
    oAuthStepRef.value = "success";
    successMessage.value = `${accountType.charAt(0).toUpperCase() + accountType.slice(1)} account authenticated successfully!`;

    // Clear session storage
    sessionStorage.removeItem("oauth-account-type");

    // Redirect back to return URL if available, otherwise stay here
    if (returnUrl) {
      sessionStorage.removeItem("oauth-return-url");
      // Small delay to show success message before redirecting
      setTimeout(() => {
        window.location.href = returnUrl;
      }, 1500);
    } else {
      // Reset success message after 3 seconds if not redirecting
      setTimeout(() => {
        successMessage.value = null;
        oAuthStepRef.value = "idle";
      }, 3000);
    }
  } else {
    oAuthStepRef.value = "error";
    errorMessage.value = `Failed to authenticate ${accountType} account. Please try again.`;
  }
}

// Initialize setup on component mount
onMounted(async () => {
  await loadSetupStatus();

  // Check if this is an OAuth callback
  if (window.location.search.includes("accountType")) {
    await handleOAuthCallback();
  }
});
</script>

<template>
  <div class="twitch-oauth-setup">
    <!-- Status Messages -->
    <div v-if="errorMessage" class="alert alert-error" role="alert">
      <span class="alert-icon">⚠️</span>
      <span>{{ errorMessage }}</span>
    </div>

    <div v-if="successMessage" class="alert alert-success" role="alert">
      <span class="alert-icon">✅</span>
      <span>{{ successMessage }}</span>
    </div>

    <!-- Setup Status Overview -->
    <div class="setup-overview">
      <h2 class="section-title">Twitch OAuth Setup</h2>
      <p class="section-description">
        Authenticate your Twitch bot and broadcaster accounts to enable chat, whispers, and channel rewards integration.
      </p>

      <div class="auth-status-grid">
        <!-- Bot Account Card -->
        <div class="auth-card" :class="{ configured: botConfigured }">
          <div class="card-header">
            <h3 class="card-title">
              <span class="account-icon">🤖</span>
              Bot Account
            </h3>
            <div class="status-badge" :class="{ active: botConfigured }">
              {{ botConfigured ? "✓ Configured" : "○ Not configured" }}
            </div>
          </div>

          <div class="card-content">
            <div v-if="botConfigured && setupData?.botTokens" class="token-info">
              <div class="token-field">
                <span class="token-label">Login:</span>
                <span class="token-value">{{ setupData.botTokens.login }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">User ID:</span>
                <span class="token-value">{{ setupData.botTokens.userId }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">Expires:</span>
                <span class="token-value">{{ formatExpiration(setupData.botTokens.expiresAtTicks) }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">Refresh Token:</span>
                <span class="token-value">{{ setupData.botTokens.hasRefreshToken ? "Yes" : "No" }}</span>
              </div>
            </div>
            <div v-else class="token-placeholder">
              <p>No bot account authenticated yet.</p>
              <p class="note">Required for: Chat messages, Whispers</p>
            </div>

            <button
              class="btn btn-primary"
              :disabled="botOAuthStep !== 'idle'"
              :class="{ loading: botOAuthStep === 'loading' }"
              @click="startBotOAuth"
            >
              <span v-if="botOAuthStep === 'loading'" class="spinner" aria-hidden="true"></span>
              {{ botConfigured ? "Re-authenticate Bot" : "Authenticate Bot" }}
            </button>
          </div>
        </div>

        <!-- Broadcaster Account Card -->
        <div class="auth-card" :class="{ configured: broadcasterConfigured }">
          <div class="card-header">
            <h3 class="card-title">
              <span class="account-icon">📺</span>
              Broadcaster Account
            </h3>
            <div class="status-badge" :class="{ active: broadcasterConfigured }">
              {{ broadcasterConfigured ? "✓ Configured" : "○ Optional" }}
            </div>
          </div>

          <div class="card-content">
            <div v-if="broadcasterConfigured && setupData?.broadcasterTokens" class="token-info">
              <div class="token-field">
                <span class="token-label">Login:</span>
                <span class="token-value">{{ setupData.broadcasterTokens.login }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">User ID:</span>
                <span class="token-value">{{ setupData.broadcasterTokens.userId }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">Expires:</span>
                <span class="token-value">{{ formatExpiration(setupData.broadcasterTokens.expiresAtTicks) }}</span>
              </div>
              <div class="token-field">
                <span class="token-label">Refresh Token:</span>
                <span class="token-value">{{ setupData.broadcasterTokens.hasRefreshToken ? "Yes" : "No" }}</span>
              </div>
            </div>
            <div v-else class="token-placeholder">
              <p>No broadcaster account authenticated yet.</p>
              <p class="note">Required for: Channel Points Redeems</p>
            </div>

            <button
              class="btn btn-secondary"
              :disabled="broadcasterOAuthStep !== 'idle'"
              :class="{ loading: broadcasterOAuthStep === 'loading' }"
              @click="startBroadcasterOAuth"
            >
              <span v-if="broadcasterOAuthStep === 'loading'" class="spinner" aria-hidden="true"></span>
              {{ broadcasterConfigured ? "Re-authenticate Broadcaster" : "Authenticate Broadcaster" }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Setup Requirements -->
    <div class="setup-requirements">
      <h3 class="subsection-title">OAuth Scopes Required</h3>
      <div class="scopes-grid">
        <div class="scope-item">
          <span class="scope-icon">🤖</span>
          <div>
            <h4>Bot Account</h4>
            <ul class="scope-list">
              <li><code>user:read:chat</code></li>
              <li><code>user:write:chat</code></li>
              <li><code>user:manage:whispers</code></li>
              <li><code>user:bot</code></li>
            </ul>
          </div>
        </div>
        <div class="scope-item">
          <span class="scope-icon">📺</span>
          <div>
            <h4>Broadcaster Account</h4>
            <ul class="scope-list">
              <li><code>channel:read:subscriptions</code></li>
              <li><code>channel:manage:redemptions</code></li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.twitch-oauth-setup {
  display: flex;
  flex-direction: column;
  gap: var(--space-lg);
}

/* ── Alerts ────────────────────────────────────────────────────────────────── */
.alert {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  padding: var(--space-md) var(--space-lg);
  border-radius: var(--radius-md);
  border-left: 4px solid;
  font-size: var(--text-sm);
  animation: slideInDown 0.3s ease-out;
}

@keyframes slideInDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.alert-error {
  background-color: color-mix(in srgb, #ef4444 15%, var(--surface-color));
  border-left-color: #ef4444;
  color: #7f1d1d;
}

.alert-success {
  background-color: color-mix(in srgb, #22c55e 15%, var(--surface-color));
  border-left-color: #22c55e;
  color: #15803d;
}

.alert-icon {
  flex-shrink: 0;
  font-size: var(--text-base);
}

/* ── Setup Overview ────────────────────────────────────────────────────────── */
.setup-overview {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
}

.section-title {
  margin: 0;
  font-size: var(--text-xl);
  font-weight: 700;
  color: var(--text-primary);
}

.section-description {
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.5;
}

.auth-status-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--space-md);
}

/* ── Auth Card ─────────────────────────────────────────────────────────────── */
.auth-card {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
  padding: var(--space-lg);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--surface-color);
  transition: all var(--transition-fast);
}

.auth-card.configured {
  border-color: color-mix(in srgb, #22c55e 50%, var(--border-color));
  background: color-mix(in srgb, #22c55e 5%, var(--surface-color));
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
}

.card-title {
  margin: 0;
  display: flex;
  align-items: center;
  gap: var(--space-sm);
  font-size: var(--text-lg);
  font-weight: 600;
  color: var(--text-primary);
}

.account-icon {
  font-size: var(--text-2xl);
  line-height: 1;
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  border-radius: var(--radius-full);
  background: color-mix(in srgb, #ef4444 15%, var(--bg-color));
  color: #991b1b;
  font-size: var(--text-xs);
  font-weight: 600;
  white-space: nowrap;
}

.status-badge.active {
  background: color-mix(in srgb, #22c55e 15%, var(--bg-color));
  color: #15803d;
}

.card-content {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
}

.token-info {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  padding: var(--space-md);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--bg-color) 50%, transparent);
}

.token-field {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  font-size: var(--text-sm);
}

.token-label {
  font-weight: 600;
  color: var(--text-secondary);
}

.token-value {
  font-family: var(--font-mono);
  color: var(--text-primary);
  word-break: break-all;
}

.token-placeholder {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  padding: var(--space-md);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--bg-color) 50%, transparent);
  text-align: center;
}

.token-placeholder p {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--text-sm);
}

.token-placeholder .note {
  color: var(--text-muted);
  font-size: var(--text-xs);
}

/* ── Buttons ───────────────────────────────────────────────────────────────── */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-xs);
  padding: 10px 16px;
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  font-size: var(--text-sm);
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
  white-space: nowrap;
  user-select: none;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn.loading {
  opacity: 0.8;
  cursor: wait;
}

.btn-primary {
  background: color-mix(in srgb, #8b5cf6 100%, transparent);
  color: white;
  border-color: color-mix(in srgb, #8b5cf6 80%, transparent);
}

.btn-primary:hover:not(:disabled) {
  background: color-mix(in srgb, #7c3aed 100%, transparent);
  border-color: color-mix(in srgb, #7c3aed 80%, transparent);
}

.btn-secondary {
  background: color-mix(in srgb, #6366f1 100%, transparent);
  color: white;
  border-color: color-mix(in srgb, #6366f1 80%, transparent);
}

.btn-secondary:hover:not(:disabled) {
  background: color-mix(in srgb, #4f46e5 100%, transparent);
  border-color: color-mix(in srgb, #4f46e5 80%, transparent);
}

/* ── Spinner ───────────────────────────────────────────────────────────────── */
.spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* ── Setup Requirements ────────────────────────────────────────────────────── */
.setup-requirements {
  padding: var(--space-lg);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  background: var(--surface-color);
}

.subsection-title {
  margin: 0 0 var(--space-md) 0;
  font-size: var(--text-lg);
  font-weight: 600;
  color: var(--text-primary);
}

.scopes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--space-md);
}

.scope-item {
  display: flex;
  gap: var(--space-sm);
  padding: var(--space-md);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--bg-color) 50%, transparent);
}

.scope-icon {
  flex-shrink: 0;
  font-size: var(--text-2xl);
  line-height: 1;
}

.scope-item h4 {
  margin: 0 0 var(--space-xs) 0;
  font-size: var(--text-base);
  color: var(--text-primary);
}

.scope-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-xs);
}

.scope-list li {
  color: var(--text-secondary);
  font-size: var(--text-sm);
}

.scope-list code {
  display: inline-block;
  padding: 2px 4px;
  border-radius: 3px;
  background: color-mix(in srgb, var(--accent-primary) 20%, transparent);
  color: var(--accent-primary);
  font-family: var(--font-mono);
  font-size: 0.85em;
}

/* ── Responsive ────────────────────────────────────────────────────────────── */
@media (max-width: 640px) {
  .auth-status-grid {
    grid-template-columns: 1fr;
  }

  .card-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .scopes-grid {
    grid-template-columns: 1fr;
  }
}
</style>
