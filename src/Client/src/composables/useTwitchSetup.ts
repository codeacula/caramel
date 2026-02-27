import { ref } from "vue";

export interface TwitchAccountTokens {
  userId: string;
  login: string;
  hasRefreshToken: boolean;
  expiresAtTicks: number;
}

export interface TwitchSetupStatus {
  isConfigured: boolean;
  botLogin: string | null;
  botTokens: TwitchAccountTokens | null;
  broadcasterTokens: TwitchAccountTokens | null;
  channelLogins: string[] | null;
}

export type SetupRequestStatus = "idle" | "loading" | "saving" | "success" | "error";

export function useTwitchSetup() {
  const setupStatus = ref<TwitchSetupStatus | null>(null);
  const requestStatus = ref<SetupRequestStatus>("idle");
  const errorMessage = ref<string | null>(null);

  /**
   * Fetch the current Twitch setup status from the backend.
   * This includes bot/broadcaster token information and channel configuration.
   */
  async function fetchSetupStatus(): Promise<TwitchSetupStatus | null> {
    requestStatus.value = "loading";
    errorMessage.value = null;

    try {
      const response = await fetch("/twitch/setup");

      if (response.ok) {
        setupStatus.value = (await response.json()) as TwitchSetupStatus;
        requestStatus.value = "idle";
        return setupStatus.value;
      }

      let detail = `Server responded with ${response.status}.`;
      try {
        const body = await response.json();
        if (typeof body?.detail === "string") detail = body.detail;
        else if (typeof body?.title === "string") detail = body.title;
      } catch {
        // Ignore parse errors
      }

      errorMessage.value = detail;
      requestStatus.value = "error";
      return null;
    } catch (err) {
      errorMessage.value = err instanceof Error ? err.message : "Failed to fetch setup status";
      requestStatus.value = "error";
      return null;
    }
  }

  /**
   * Submit setup configuration (bot login + channels).
   * This is called after OAuth authentication to complete the setup.
   */
  async function submitSetup(botLogin: string, channelLogins: string[]): Promise<boolean> {
    requestStatus.value = "saving";
    errorMessage.value = null;

    try {
      const response = await fetch("/twitch/setup", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ botLogin, channelLogins }),
      });

      if (response.ok) {
        setupStatus.value = (await response.json()) as TwitchSetupStatus;
        requestStatus.value = "success";
        return true;
      }

      let detail = `Server responded with ${response.status}.`;
      try {
        const body = await response.json();
        if (typeof body?.detail === "string") detail = body.detail;
        else if (typeof body?.title === "string") detail = body.title;
      } catch {
        // Ignore parse errors
      }

      errorMessage.value = detail;
      requestStatus.value = "error";
      return false;
    } catch (err) {
      errorMessage.value = err instanceof Error ? err.message : "Failed to save setup";
      requestStatus.value = "error";
      return false;
    }
  }

  return {
    setupStatus,
    requestStatus,
    errorMessage,
    fetchSetupStatus,
    submitSetup,
  };
}
