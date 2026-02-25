import { ref } from 'vue';

const isConnected = ref(false);
const currentScene = ref<string | null>(null);

export function useObs() {
  const refreshStatus = async () => {
    try {
      const response = await fetch('/api/obs/status');
      if (!response.ok) {
        console.error('Failed to fetch OBS status', response.status);
        isConnected.value = false;
        currentScene.value = null;
        return;
      }
      const data = await response.json();
      isConnected.value = data.isConnected;
      currentScene.value = data.currentScene;
    } catch (error) {
      console.error('Failed to fetch OBS status', error);
      isConnected.value = false;
      currentScene.value = null;
    }
  };

  const switchToScene = async (sceneName: string) => {
    try {
      const response = await fetch('/api/obs/scene', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sceneName }),
      });
      if (!response.ok) {
        console.error(`Failed to switch to scene ${sceneName}`, response.status);
        return;
      }
      await refreshStatus();
    } catch (error) {
      console.error(`Failed to switch to scene ${sceneName}`, error);
    }
  };

  return {
    isConnected,
    refreshStatus,
    switchToScene,
  };
}
