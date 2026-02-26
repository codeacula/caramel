import { ref } from "vue";

interface RunAdsRequest {
  duration: number;
}

export function useTwitchAds() {
  const adsStatus = ref<"idle" | "loading" | "success" | "error">("idle");
  const adsError = ref<string | null>(null);

  async function runAds(duration: number = 30): Promise<boolean> {
    adsStatus.value = "loading";
    adsError.value = null;

    try {
      const response = await fetch("/twitch/ads/run", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ duration } as RunAdsRequest),
      });

      if (response.ok) {
        adsStatus.value = "success";
        return true;
      }

      const errorData = await response.json();
      adsError.value =
        errorData.message || "Failed to run ads on Twitch";
      adsStatus.value = "error";
      return false;
    } catch (error) {
      adsError.value =
        error instanceof Error
          ? error.message
          : "An unexpected error occurred";
      adsStatus.value = "error";
      return false;
    }
  }

  return {
    adsStatus,
    adsError,
    runAds,
  };
}
