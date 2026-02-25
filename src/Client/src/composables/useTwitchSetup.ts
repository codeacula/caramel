import { ref } from "vue";

export interface TwitchSetupStatus {
  isConfigured: boolean;
  botLogin: string | null;
  channelLogins: string[] | null;
}

export type SetupRequestStatus = "idle" | "loading" | "saving" | "success" | "error";

export function useTwitchSetup() {
  const setupStatus = ref<TwitchSetupStatus | null>(null);
  const requestStatus = ref<SetupRequestStatus>("idle");
  const errorMessage = ref<string | null>(null);

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
    requestStatus,
    errorMessage,
    submitSetup,
  };
}
