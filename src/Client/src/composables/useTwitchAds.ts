import { ref } from "vue";

// Module-level singleton state (shared across all callers, like useObs)
const adsStatus = ref<"idle" | "loading" | "success" | "error">("idle");
const adsError = ref<string | null>(null);
const cooldownRemaining = ref(0);

let cooldownInterval: ReturnType<typeof setInterval> | null = null;

function startCooldown(seconds: number) {
  if (cooldownInterval !== null) {
    clearInterval(cooldownInterval);
    cooldownInterval = null;
  }

  if (seconds <= 0) {
    cooldownRemaining.value = 0;
    return;
  }

  cooldownRemaining.value = seconds;
  cooldownInterval = setInterval(() => {
    cooldownRemaining.value -= 1;
    if (cooldownRemaining.value <= 0) {
      cooldownRemaining.value = 0;
      if (cooldownInterval !== null) {
        clearInterval(cooldownInterval);
        cooldownInterval = null;
      }
    }
  }, 1000);
}

export function useTwitchAds() {
  async function runAds(duration: number = 180): Promise<{ success: boolean; retryAfter: number; errorMessage: string | null }> {
    adsStatus.value = "loading";
    adsError.value = null;

    try {
      const response = await fetch("/twitch/ads/run", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ duration }),
      });

      const data = await response.json().catch(() => ({}));

      if (response.ok) {
        const retryAfter: number = typeof data.retry_after === "number" ? data.retry_after : 0;
        startCooldown(retryAfter);
        adsStatus.value = "success";
        return { success: true, retryAfter, errorMessage: null };
      }

      // 409 = on cooldown — parse retry_after from backend
      if (response.status === 409) {
        const retryAfter: number = typeof data.retry_after === "number" ? data.retry_after : 0;
        startCooldown(retryAfter);
        adsStatus.value = "error";
        const msg = data.message ?? "Ads are on cooldown.";
        adsError.value = msg;
        return { success: false, retryAfter, errorMessage: msg };
      }

      const msg = data.detail ?? data.message ?? "Failed to run ads on Twitch";
      adsError.value = msg;
      adsStatus.value = "error";
      return { success: false, retryAfter: 0, errorMessage: msg };
    } catch (error) {
      const msg = error instanceof Error ? error.message : "An unexpected error occurred";
      adsError.value = msg;
      adsStatus.value = "error";
      return { success: false, retryAfter: 0, errorMessage: msg };
    }
  }

  return {
    adsStatus,
    adsError,
    cooldownRemaining,
    runAds,
  };
}
